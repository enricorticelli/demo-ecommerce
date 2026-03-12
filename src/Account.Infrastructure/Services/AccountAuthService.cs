using Account.Application.Abstractions.Services;
using Account.Application.Inputs;
using Account.Application.Models;
using Account.Domain.Entities;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Mappers;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Helpers;

namespace Account.Infrastructure.Services;

public sealed class AccountAuthService(
    AccountDbContext dbContext,
    TokenFactory tokenFactory,
    OrderApiClient orderApiClient,
    AccountTechnicalOptions options,
    IHostEnvironment hostEnvironment,
    ILogger<AccountAuthService> logger) : IAccountAuthService
{
    private static readonly string[] CustomerPermissions = AuthorizationPermissions.AllCustomer;

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
            .FirstOrDefaultAsync(x => x.Realm == realm && (x.NormalizedEmail == normalized || x.Username == normalized), cancellationToken);

        if (userEntity is null)
        {
            LogAuthWarning("login", realm, null, "invalid_credentials");
            throw new ValidationAppException("Invalid credentials.");
        }

        var domainUser = AccountUserEntityMapper.ToDomain(userEntity);
        if (!PasswordHasher.VerifyPassword(request.Password, domainUser.PasswordHash))
        {
            LogAuthWarning("login", realm, domainUser.Id, "invalid_credentials");
            throw new ValidationAppException("Invalid credentials.");
        }

        var tokens = await IssueTokensAsync(userEntity, realm, cancellationToken);

        if (realm == AccountRealm.Customer && domainUser.IsEmailVerified)
        {
            await orderApiClient.ClaimGuestOrdersAsync(tokens.AccessToken, domainUser.Email, cancellationToken);
        }

        LogAuthInformation("login", realm, domainUser.Id, "success");

        return tokens;
    }

    public async Task<AuthTokenResult> RefreshAsync(string realm, string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            LogAuthWarning("refresh", realm, null, "missing_refresh_token");
            throw new ValidationAppException("Refresh token is required.");
        }

        var refreshHash = PasswordHasher.HashToken(refreshToken.Trim());
        var session = await dbContext.RefreshSessions
            .FirstOrDefaultAsync(x => x.Realm == realm && x.TokenHash == refreshHash, cancellationToken)
            ;

        if (session is null)
        {
            LogAuthWarning("refresh", realm, null, "invalid_refresh_token");
            throw new ValidationAppException("Invalid refresh token.");
        }

        if (session.RevokedAtUtc.HasValue || session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            LogAuthWarning("refresh", realm, session.UserId, "expired_or_revoked_refresh_token");
            throw new ValidationAppException("Refresh token expired or revoked.");
        }

        session.RevokedAtUtc = DateTimeOffset.UtcNow;

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == session.UserId && x.Realm == realm, cancellationToken)
            ;

        if (user is null)
        {
            LogAuthWarning("refresh", realm, session.UserId, "user_not_found");
            throw new NotFoundAppException("Account not found.");
        }

        var tokens = await IssueTokensAsync(user, realm, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogAuthInformation("refresh", realm, user.Id, "success");

        return tokens;
    }

    public async Task LogoutAsync(string realm, string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            LogAuthInformation("logout", realm, null, "noop_missing_refresh_token");
            return;
        }

        var refreshHash = PasswordHasher.HashToken(refreshToken.Trim());
        var session = await dbContext.RefreshSessions
            .FirstOrDefaultAsync(x => x.Realm == realm && x.TokenHash == refreshHash, cancellationToken);

        if (session is null)
        {
            LogAuthInformation("logout", realm, null, "noop_session_not_found");
            return;
        }

        session.RevokedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        LogAuthInformation("logout", realm, session.UserId, "success");
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
        return (true, ToPreviewCode(code));
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

        await orderApiClient.ClaimGuestOrdersInternalAsync(
            domainUser.Id,
            domainUser.Email,
            options.OrderInternalApiKey,
            cancellationToken);

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
        return (true, ToPreviewCode(code));
    }

    public async Task ResetPasswordAsync(ResetPasswordInput request, CancellationToken cancellationToken)
    {
        ValidatePassword(request.NewPassword);
        var normalized = NormalizeLoginIdentity(request.Email);
        var userEntity = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken);

        if (userEntity is null)
        {
            LogAuthWarning("reset_password", AccountRealm.Customer, null, "invalid_reset_request");
            throw new ValidationAppException("Invalid password reset request.");
        }

        var token = await dbContext.PasswordResetTokens
            .Where(x => x.UserId == userEntity.Id && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (token is null)
        {
            LogAuthWarning("reset_password", AccountRealm.Customer, userEntity.Id, "missing_reset_code");
            throw new ValidationAppException("No active reset code.");
        }

        if (token.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            LogAuthWarning("reset_password", AccountRealm.Customer, userEntity.Id, "expired_reset_code");
            throw new ValidationAppException("Reset code expired.");
        }

        if (!string.Equals(token.CodeHash, PasswordHasher.HashToken(request.Code.Trim()), StringComparison.Ordinal))
        {
            LogAuthWarning("reset_password", AccountRealm.Customer, userEntity.Id, "invalid_reset_code");
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
        LogAuthInformation("reset_password", AccountRealm.Customer, userEntity.Id, "success");
    }

    private async Task<AuthTokenResult> IssueTokensAsync(AccountUserEntity user, string realm, CancellationToken cancellationToken)
    {
        var permissions = GetPermissionsForUser(user, realm);
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

    private static string[] GetPermissionsForUser(AccountUserEntity user, string realm)
    {
        if (realm != AccountRealm.Admin)
        {
            return CustomerPermissions;
        }

        if (user.CustomPermissions is not null)
        {
            return user.CustomPermissions;
        }

        return AuthorizationPermissions.AllAdmin;
    }

    private string? ToPreviewCode(string code)
    {
        return hostEnvironment.IsDevelopment() ? code : null;
    }

    private void LogAuthInformation(string eventName, string realm, Guid? userId, string outcome)
    {
        logger.LogInformation(
            "Auth audit event={EventName} realm={Realm} userId={UserId} outcome={Outcome}",
            eventName,
            realm,
            userId,
            outcome);
    }

    private void LogAuthWarning(string eventName, string realm, Guid? userId, string outcome)
    {
        logger.LogWarning(
            "Auth audit event={EventName} realm={Realm} userId={UserId} outcome={Outcome}",
            eventName,
            realm,
            userId,
            outcome);
    }
}
