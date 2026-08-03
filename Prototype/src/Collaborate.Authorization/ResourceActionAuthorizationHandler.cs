using Microsoft.AspNetCore.Authorization;

namespace Collaborate.Authorization;

/// <summary>Resource-level check, run via <c>IAuthorizationService.AuthorizeAsync(user, resource, policy)</c>.
/// The predicate is the only thing that decides; this handler just wires claims and the snapshot to it.</summary>
public sealed class ResourceActionAuthorizationHandler : AuthorizationHandler<ResourceActionRequirement, IWorkspaceResource>
{
    private readonly SnapshotAccessor _accessor;

    public ResourceActionAuthorizationHandler(SnapshotAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceActionRequirement requirement,
        IWorkspaceResource resource)
    {
        var userId = context.User.FindFirst(SnapshotClaimTypes.UserId)?.Value;
        var sid = context.User.FindFirst(SnapshotClaimTypes.SessionId)?.Value;
        if (userId is null || sid is null)
        {
            return;
        }

        var result = await _accessor.GetAsync(resource.WorkspaceId, userId, sid, CancellationToken.None);

        if (PermissionEvaluator.IsAllowed(result.Snapshot, result.SessionRevoked, requirement.Action, resource.ResourceId))
        {
            context.Succeed(requirement);
        }
    }
}
