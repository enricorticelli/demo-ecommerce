using Microsoft.AspNetCore.Authorization;

namespace Gateway.Api.Security;

public sealed class CapabilityAuthorizationHandler : AuthorizationHandler<CapabilityAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CapabilityAuthorizationRequirement requirement)
    {
        var capabilities = context.User.FindAll(CapabilitySecurity.ClaimType)
            .Select(static claim => claim.Value);

        if (CapabilitySecurity.IsSatisfiedBy(capabilities, requirement.Capability))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
