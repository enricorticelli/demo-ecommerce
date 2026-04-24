using Microsoft.AspNetCore.Authorization;

namespace Gateway.Api.Security;

public sealed class CapabilityAuthorizationRequirement(string capability) : IAuthorizationRequirement
{
    public string Capability { get; } = capability;
}
