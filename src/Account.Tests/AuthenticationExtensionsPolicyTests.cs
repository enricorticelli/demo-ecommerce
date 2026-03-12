using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.BuildingBlocks.Api;
using Xunit;

namespace Account.Tests;

public sealed class AuthenticationExtensionsPolicyTests
{
    [Fact]
    public async Task AddAdminAuthentication_RegistersAdminPermissionPolicies()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddAdminAuthentication();

        await using var app = builder.Build();
        var provider = app.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync(AuthorizationPolicies.CatalogWritePolicy);

        Assert.NotNull(policy);
        AssertPolicyHasClaim(policy!, AuthorizationClaimTypes.Realm, "admin");
        AssertPolicyHasClaim(policy!, AuthorizationClaimTypes.Permission, AuthorizationPermissions.CatalogWrite);
    }

    [Fact]
    public async Task AddStoreAndAdminAuthentication_RegistersOrdersWritePolicy()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddStoreAndAdminAuthentication();

        await using var app = builder.Build();
        var provider = app.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync(AuthorizationPolicies.OrdersWritePolicy);

        Assert.NotNull(policy);
        AssertPolicyHasClaim(policy!, AuthorizationClaimTypes.Realm, "admin");
        AssertPolicyHasClaim(policy!, AuthorizationClaimTypes.Permission, AuthorizationPermissions.OrdersWrite);
    }

    private static void AssertPolicyHasClaim(AuthorizationPolicy policy, string claimType, string claimValue)
    {
        var requirement = Assert.Single(
            policy.Requirements.OfType<ClaimsAuthorizationRequirement>(),
            x => x.ClaimType == claimType && (x.AllowedValues?.Contains(claimValue) ?? false));

        Assert.Equal(claimType, requirement.ClaimType);
    }
}
