using Account.Application.Inputs;
using Account.Application.Models;
using Account.Infrastructure.Configuration;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.Entities;
using Account.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Exceptions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Account.Tests;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task RegisterCustomerAsync_ValidInput_PersistsUserAndReturnsTokens()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateAuthService(dbContext);

        var result = await service.RegisterCustomerAsync(
            new RegisterCustomerInput("mario.rossi@example.com", "Password123", "Mario", "Rossi", "+39 333 1112222"),
            CancellationToken.None);

        Assert.Equal(AccountRealm.Customer, result.Realm);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

        var user = await dbContext.Users.SingleAsync();
        Assert.Equal("customer", user.Realm);
        Assert.Equal("mario.rossi@example.com", user.NormalizedEmail);

        var verificationToken = await dbContext.EmailVerificationTokens.SingleAsync();
        Assert.Equal(user.Id, verificationToken.UserId);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsValidationException()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = Guid.NewGuid(),
            Realm = "customer",
            Username = "mario.rossi@example.com",
            Email = "mario.rossi@example.com",
            NormalizedEmail = "mario.rossi@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Mario",
            LastName = "Rossi",
            Phone = "+39",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.LoginAsync(AccountRealm.Customer, new LoginInput("mario.rossi@example.com", "WrongPassword"), CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_AdminRealm_WithCustomPermissions_ReturnsOnlyCustomPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = Guid.NewGuid(),
            Realm = AccountRealm.Admin,
            Username = "admin-custom",
            Email = "admin-custom@example.com",
            NormalizedEmail = "admin-custom@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Admin",
            LastName = "Custom",
            Phone = "+39",
            CustomPermissions = new[] { AuthorizationPermissions.CatalogRead, AuthorizationPermissions.AccountRead },
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var result = await service.LoginAsync(AccountRealm.Admin, new LoginInput("admin-custom", "Password123"), CancellationToken.None);

        Assert.Equal(2, result.Permissions.Length);
        Assert.Contains(AuthorizationPermissions.CatalogRead, result.Permissions);
        Assert.Contains(AuthorizationPermissions.AccountRead, result.Permissions);
    }

    [Fact]
    public async Task CreateAdminByAdminAsync_WithCustomPermissions_PersistsCustomSet()
    {
        await using var dbContext = CreateDbContext();
        var options = new AccountTechnicalOptions
        {
            DefaultAdminUsername = "admin"
        };

        var actingSuperUserId = Guid.NewGuid();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = actingSuperUserId,
            Realm = AccountRealm.Admin,
            Username = "admin",
            Email = "admin@example.com",
            NormalizedEmail = "admin@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Default",
            LastName = "Admin",
            Phone = "+39",
            IsSuperUser = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new AccountAdministrationService(dbContext, options);

        var created = await service.CreateAdminByAdminAsync(
            actingSuperUserId,
            new CreateAdminInput(
                "ops-admin",
                "Password123",
                new[] { AuthorizationPermissions.ShippingRead, AuthorizationPermissions.ShippingWrite, AuthorizationPermissions.AccountRead },
                false),
            CancellationToken.None);

        Assert.Equal("ops-admin", created.Username);
        Assert.True(created.HasCustomPermissions);
        Assert.Contains(AuthorizationPermissions.ShippingRead, created.Permissions);
        Assert.Contains(AuthorizationPermissions.ShippingWrite, created.Permissions);
        Assert.Contains(AuthorizationPermissions.AccountRead, created.Permissions);

        var persisted = await dbContext.Users.SingleAsync(x => x.Id == created.Id);
        Assert.NotNull(persisted.CustomPermissions);
        Assert.Equal(3, persisted.CustomPermissions!.Length);
    }

    [Fact]
    public async Task LoginAsync_AdminRealm_ReturnsExpandedAdminPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = Guid.NewGuid(),
            Realm = AccountRealm.Admin,
            Username = "admin",
            Email = "admin@example.com",
            NormalizedEmail = "admin@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Admin",
            LastName = "User",
            Phone = "+39",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var result = await service.LoginAsync(AccountRealm.Admin, new LoginInput("admin", "Password123"), CancellationToken.None);

        Assert.Contains(AuthorizationPermissions.CatalogRead, result.Permissions);
        Assert.Contains(AuthorizationPermissions.CatalogWrite, result.Permissions);
        Assert.Contains(AuthorizationPermissions.OrdersRead, result.Permissions);
        Assert.Contains(AuthorizationPermissions.OrdersWrite, result.Permissions);
        Assert.Contains(AuthorizationPermissions.ShippingRead, result.Permissions);
        Assert.Contains(AuthorizationPermissions.ShippingWrite, result.Permissions);
        Assert.Contains(AuthorizationPermissions.WarehouseRead, result.Permissions);
        Assert.Contains(AuthorizationPermissions.WarehouseWrite, result.Permissions);
        Assert.Contains(AuthorizationPermissions.AccountRead, result.Permissions);
        Assert.Contains(AuthorizationPermissions.AccountWrite, result.Permissions);
    }

    [Fact]
    public async Task LoginAsync_AdminRealm_DefaultAdminWithCustomPermissions_StillHasAllPermissions()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = Guid.NewGuid(),
            Realm = AccountRealm.Admin,
            Username = "admin",
            Email = "admin@example.com",
            NormalizedEmail = "admin@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Default",
            LastName = "Admin",
            Phone = "+39",
            CustomPermissions = new[] { AuthorizationPermissions.CatalogRead },
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext);

        var result = await service.LoginAsync(AccountRealm.Admin, new LoginInput("admin", "Password123"), CancellationToken.None);

        Assert.Equal(AuthorizationPermissions.AllAdmin.Length, result.Permissions.Length);
        foreach (var permission in AuthorizationPermissions.AllAdmin)
        {
            Assert.Contains(permission, result.Permissions);
        }
    }

    [Fact]
    public async Task ListAdminsAsync_NonSuperUser_ThrowsForbidden()
    {
        await using var dbContext = CreateDbContext();
        var options = new AccountTechnicalOptions
        {
            DefaultAdminUsername = "admin"
        };

        var normalAdminId = Guid.NewGuid();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = normalAdminId,
            Realm = AccountRealm.Admin,
            Username = "ops-admin",
            Email = "ops-admin@example.com",
            NormalizedEmail = "ops-admin@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Ops",
            LastName = "Admin",
            Phone = "+39",
            IsSuperUser = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new AccountAdministrationService(dbContext, options);

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            service.ListAdminsAsync(normalAdminId, 20, 0, null, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePasswordResetCodeAsync_InProduction_ReturnsNullPreviewCode()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = Guid.NewGuid(),
            Realm = AccountRealm.Customer,
            Username = "mario.rossi@example.com",
            Email = "mario.rossi@example.com",
            NormalizedEmail = "mario.rossi@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Mario",
            LastName = "Rossi",
            Phone = "+39",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext, environmentName: Environments.Production);

        var result = await service.CreatePasswordResetCodeAsync(new ForgotPasswordInput("mario.rossi@example.com"), CancellationToken.None);

        Assert.True(result.Issued);
        Assert.Null(result.PreviewCode);
    }

    [Fact]
    public async Task CreatePasswordResetCodeAsync_InDevelopment_ReturnsPreviewCode()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = Guid.NewGuid(),
            Realm = AccountRealm.Customer,
            Username = "mario.rossi@example.com",
            Email = "mario.rossi@example.com",
            NormalizedEmail = "mario.rossi@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Mario",
            LastName = "Rossi",
            Phone = "+39",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext, environmentName: Environments.Development);

        var result = await service.CreatePasswordResetCodeAsync(new ForgotPasswordInput("mario.rossi@example.com"), CancellationToken.None);

        Assert.True(result.Issued);
        Assert.False(string.IsNullOrWhiteSpace(result.PreviewCode));
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_WritesAuditWarning()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = Guid.NewGuid(),
            Realm = AccountRealm.Customer,
            Username = "mario.rossi@example.com",
            Email = "mario.rossi@example.com",
            NormalizedEmail = "mario.rossi@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Mario",
            LastName = "Rossi",
            Phone = "+39",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var logger = new TestLogger<AccountAuthService>();
        var service = CreateAuthService(dbContext, logger: logger);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.LoginAsync(AccountRealm.Customer, new LoginInput("mario.rossi@example.com", "WrongPassword"), CancellationToken.None));

        Assert.Contains(logger.Entries, x =>
            x.Level == LogLevel.Warning &&
            x.Message.Contains("event=login", StringComparison.OrdinalIgnoreCase) &&
            x.Message.Contains("outcome=invalid_credentials", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidCode_SetsUserAsVerified()
    {
        await using var dbContext = CreateDbContext();
        var handler = new StubHttpMessageHandler();
        var userId = Guid.NewGuid();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = userId,
            Realm = "customer",
            Username = "mario.rossi@example.com",
            Email = "mario.rossi@example.com",
            NormalizedEmail = "mario.rossi@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = false,
            FirstName = "Mario",
            LastName = "Rossi",
            Phone = "+39",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        var code = "123456";
        dbContext.EmailVerificationTokens.Add(new EmailVerificationTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = PasswordHasher.HashToken(code),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = CreateAuthService(dbContext, handler);

        await service.VerifyEmailAsync(new VerifyEmailInput("mario.rossi@example.com", code), CancellationToken.None);

        var user = await dbContext.Users.SingleAsync();
        Assert.True(user.IsEmailVerified);

        var internalClaimCall = Assert.Single(handler.Requests, x => x.RequestUri?.AbsolutePath == "/internal/v1/orders/claim-guest");
        Assert.Equal("dev-order-internal-key-change-me", internalClaimCall.Headers.GetValues("X-Internal-Api-Key").Single());
    }

    [Fact]
    public async Task OrderApiClient_ClaimGuestOrdersAsync_UsesStoreEndpointWithBearerToken()
    {
        var handler = new StubHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sut = new OrderApiClient(httpClient);

        await sut.ClaimGuestOrdersAsync("token-123", "mario@example.com", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/store/v1/orders/claim-guest", request.RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("token-123", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task OrderApiClient_ClaimGuestOrdersInternalAsync_UsesInternalEndpointWithApiKey()
    {
        var handler = new StubHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sut = new OrderApiClient(httpClient);

        await sut.ClaimGuestOrdersInternalAsync(Guid.NewGuid(), "mario@example.com", "internal-key", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/internal/v1/orders/claim-guest", request.RequestUri?.AbsolutePath);
        Assert.Equal("internal-key", request.Headers.GetValues("X-Internal-Api-Key").Single());
    }

    [Fact]
    public async Task OrderApiClient_ListByAuthenticatedUserAsync_UsesStoreEndpointWithoutUserIdQueryAndWithBearer()
    {
        var handler = new StubHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sut = new OrderApiClient(httpClient);

        _ = await sut.ListByAuthenticatedUserAsync("token-abc", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/store/v1/orders", request.RequestUri?.AbsolutePath);
        Assert.DoesNotContain("authenticatedUserId", request.RequestUri?.Query ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("token-abc", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task CreateAddressAsync_CreatesAddressForCustomer()
    {
        await using var dbContext = CreateDbContext();
        var customerId = Guid.NewGuid();
        dbContext.Users.Add(new AccountUserEntity
        {
            Id = customerId,
            Realm = "customer",
            Username = "mario.rossi@example.com",
            Email = "mario.rossi@example.com",
            NormalizedEmail = "mario.rossi@example.com",
            PasswordHash = PasswordHasher.HashPassword("Password123"),
            IsEmailVerified = true,
            FirstName = "Mario",
            LastName = "Rossi",
            Phone = "+39",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = CreateCustomerProfileService(dbContext);

        var address = await service.CreateAddressAsync(
            customerId,
            new UpsertAddressInput("Casa", "Via Roma 1", "Milano", "20100", "Italia", true, true),
            CancellationToken.None);

        Assert.Equal("Casa", address.Label);
        Assert.Equal(customerId, address.UserId);
        Assert.Single(await dbContext.Addresses.ToListAsync());
    }

    private static AccountAuthService CreateAuthService(
        AccountDbContext dbContext,
        StubHttpMessageHandler? handler = null,
        string environmentName = "Development",
        ILogger<AccountAuthService>? logger = null)
    {
        var options = new AccountTechnicalOptions
        {
            JwtSigningKey = "dev-only-signing-key-change-me-dev-only-signing-key",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 14,
            CustomerIssuer = "account-customer",
            CustomerAudience = "storefront",
            AdminIssuer = "account-admin",
            AdminAudience = "backoffice",
            OrderApiBaseUrl = "http://localhost",
            OrderInternalApiKey = "dev-order-internal-key-change-me"
        };

        var tokenFactory = new TokenFactory(options);
        var httpClient = new HttpClient(handler ?? new StubHttpMessageHandler()) { BaseAddress = new Uri("http://localhost") };
        var orderApiClient = new OrderApiClient(httpClient);
        var environment = new TestHostEnvironment(environmentName);

        return new AccountAuthService(dbContext, tokenFactory, orderApiClient, options, environment, logger ?? NullLogger<AccountAuthService>.Instance);
    }

    private static AccountCustomerProfileService CreateCustomerProfileService(AccountDbContext dbContext)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler()) { BaseAddress = new Uri("http://localhost") };
        var orderApiClient = new OrderApiClient(httpClient);

        return new AccountCustomerProfileService(dbContext, orderApiClient);
    }

    private static AccountDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseInMemoryDatabase($"account-tests-{Guid.NewGuid()}")
            .Options;

        return new AccountDbContext(options);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (request.Method == HttpMethod.Get)
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Account.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
