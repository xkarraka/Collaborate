using Microsoft.AspNetCore.Authorization;

namespace Collaborate.Authorization;

public sealed class ResourceActionRequirement : IAuthorizationRequirement
{
    public ResourceActionRequirement(string action)
    {
        Action = action;
    }

    public string Action { get; }
}
