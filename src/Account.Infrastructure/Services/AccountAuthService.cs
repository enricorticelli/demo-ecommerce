using Account.Application.Abstractions.Services;
using Account.Application.Inputs;
using Account.Application.Models;
using Account.Domain.Entities;
using Account.Infrastructure.Mappers;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Helpers;

namespace Account.Infrastructure.Services;

public sealed class AccountAuthService(AccountDbContext dbContext, TokenFactory tokenFactory, OrderApiClient orderApiClient) : IAccountAuthService
{
    private static readonly string[] CustomerPermissions = ["account:read", "account:write", "orders:read"];
    private static readonly string[] AdminPermissions = ["catalog:read", "catalog:write", "orders:read", "shipping:read", "shipping:write"];

    public async Task<AuthTokenResult> RegisterCustomerAsync(RegisterCustomerInput request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeLoginIdentity(request.Email);
        if (await dbContext.Users.AnyAsync(x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            throw new ConflictAppException("An account with this email already exists.");
        }

        ValidatePassword(request.Password);
        var domainUser = AccountUser.CreateCustomer(
            request.Email,
            normalizedEmail,
            PasswordHasher.HashPassword(request.Password),
            request.FirstName,
            request.LastName,
            request.Phone);

        var entity = AccountUserEntityMapper.ToEntity(domainUser);
        dbContext.Users.Add(entity);

        await CreateEmailVerificationCodeAsync(entity.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(entity, AccountRealm.Customer, cancellationToken);
    }

    public async Task<AuthTokenResult> LoginAsync(string realm, LoginInput request, CancellationToken cancellationToken)
    {
        var normalized = NormalizeLoginIdentity(request.Username);
        var userEntity = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Realm == realm && (x.NormalizedEmail == normalized || x.Username == normalized), cancellationToken)
            ?? throw new ValidationAppException("Invalid credentials.");

        var domainUser = AccountUserEntityMapper.ToDomain(userEntity);
        if (!PasswordHasher.VerifyPassword(request.Password, domainUser.PasswordHash))
        {
            throw new ValidationAppException("Invalid credentials.");
        }

        if (realm == AccountRealm.Customer && domainUser.IsEmailVerified)
        {
            await orderApiClient.ClaimGuestOrdersAsync(domainUser.Id, domainUser.Email, cancellationToken);
        }

        return await IssueTokensAsync(userEntity, realm, cancellationToken);
    }

    public async Task<AuthTokenResult> RefreshAsync(string realm, string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationAppException("Refresh token is required.");
        }

        var refreshHash = PasswordHasher.HashToken(refreshToken.Trim());
        var session = await dbContext.RefreshSessions
            .FirstOrDefaultAsync(x => x.Realm == realm && x.TokenHash == refreshHash, cancellationToken)
            ?? throw new ValidationAppException("Invalid refresh token.");

        if (session.RevokedAtUtc.HasValue || session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ValidationAppException("Refresh token expired or revoked.");
        }

        session.RevokedAtUtc = DateTimeOffset.UtcNow;

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == session.UserId && x.Realm == realm, cancellationToken)
            ?? throw new NotFoundAppException("Account not found.");

        var tokens = await IssueTokensAsync(user, realm, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return tokens;
    }

    public async Task LogoutAsync(string realm, string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var refreshHash = PasswordHasher.HashToken(refreshToken.Trim());
        var session = await dbContext.RefreshSessions
            .FirstOrDefaultAsync(x => x.Realm == realm && x.TokenHash == refreshHash, cancellationToken);

        if (session is null)
        {
            return;
        }

        session.RevokedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Issued, string? PreviewCode)> CreateEmailVerificationCodeByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = NormalizeLoginIdentity(email);
        var user = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken);

        if (user is null)
        {
            return (false, null);
        }

        var code = await CreateEmailVerificationCodeAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, code);
    }

    public async Task VerifyEmailAsync(VerifyEmailInput request, CancellationToken cancellationToken)
    {
        var normalized = NormalizeLoginIdentity(request.Email);
        var userEntity = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken)
            ?? throw new ValidationAppException("Invalid verification request.");

        var token = await dbContext.EmailVerificationTokens
            .Where(x => x.UserId == userEntity.Id && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationAppException("No active verification code.");

        if (token.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ValidationAppException("Verification code expired.");
        }

        if (!string.Equals(token.CodeHash, PasswordHasher.HashToken(request.Code.Trim()), StringComparison.Ordinal))
        {
            throw new ValidationAppException("Invalid verification code.");
        }

        token.UsedAtUtc = DateTimeOffset.UtcNow;

        var domainUser = AccountUserEntityMapper.ToDomain(userEntity);
        domainUser.VerifyEmail();
        AccountUserEntityMapper.ApplyDomain(domainUser, userEntity);

        await orderApiClient.ClaimGuestOrdersAsync(domainUser.Id, domainUser.Email, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Issued, string? PreviewCode)> CreatePasswordResetCodeAsync(ForgotPasswordInput request, CancellationToken cancellationToken)
    {
        var normalized = NormalizeLoginIdentity(request.Email);
        var user = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken);

        if (user is null)
        {
            return (false, null);
        }

        var code = CodeGenerationHelper.GenerateNumericCode(6);
        dbContext.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = PasswordHasher.HashToken(code),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(20)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, code);
    }

    public async Task ResetPasswordAsync(ResetPasswordInput request, CancellationToken cancellationToken)
    {
        ValidatePassword(request.NewPassword);
        var normalized = NormalizeLoginIdentity(request.Email);
        var userEntity = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken)
            ?? throw new ValidationAppException("Invalid password reset request.");

        var token = await dbContext.PasswordResetTokens
            .Where(x => x.UserId == userEntity.Id && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationAppException("No active reset code.");

        if (token.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ValidationAppException("Reset code expired.");
        }

        if (!string.Equals(token.CodeHash, PasswordHasher.HashToken(request.Code.Trim()), StringComparison.Ordinal))
        {
            throw new ValidationAppException("Invalid reset code.");
        }

        token.UsedAtUtc = DateTimeOffset.UtcNow;

        var domainUser = AccountUserEntityMapper.ToDomain(userEntity);
        domainUser.ChangePasswordHash(PasswordHasher.HashPassword(request.NewPassword));
        AccountUserEntityMapper.ApplyDomain(domainUser, userEntity);

        var activeSessions = await dbContext.RefreshSessions.Where(x => x.UserId == userEntity.Id && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthTokenResult> IssueTokensAsync(AccountUserEntity user, string realm, CancellationToken cancellationToken)
    {
        var permissions = GetPermissionsForRealm(realm);
        var (accessToken, accessExpiresAt) = tokenFactory.CreateAccessToken(user.Id, user.Email, user.IsEmailVerified, realm, permissions);
        var (refreshToken, refreshExpiresAt) = tokenFactory.CreateRefreshToken();

        dbContext.RefreshSessions.Add(new RefreshSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Realm = realm,
            TokenHash = PasswordHasher.HashToken(refreshToken),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = refreshExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshExpiresAt,
            realm,
            user.Id,
            user.Email,
            permissions);
    }

    private async Task<string> CreateEmailVerificationCodeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var code = CodeGenerationHelper.GenerateNumericCode(6);
        dbContext.EmailVerificationTokens.Add(new EmailVerificationTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = PasswordHasher.HashToken(code),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(20),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return code;
    }

    private static string NormalizeLoginIdentity(string value)
    {
        return InputValidationHelper.NormalizeRequiredLowerInvariant(value, "Email/username");
    }

    private static void ValidatePassword(string password)
    {
        InputValidationHelper.EnsureMinLength(password, 8, "Password must be at least 8 characters long.");
    }

    private static string[] GetPermissionsForRealm(string realm)
    {
        return realm == AccountRealm.Admin ? AdminPermissions : CustomerPermissions;
    }
}
