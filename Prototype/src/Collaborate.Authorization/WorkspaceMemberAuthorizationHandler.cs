using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Collaborate.Authorization;

/// <summary>Route-level check: the route's <c>workspaceId</c> segment must have a
/// live, non-revoked snapshot for the current user. Membership is proved purely
/// by snapshot existence — no separate workspace claim on the token is needed.</summary>
public sealed class WorkspaceMemberAuthorizationHandler : AuthorizationHandler<WorkspaceMemberRequirement>
{
    private readonly SnapshotAccessor _accessor;

    public WorkspaceMemberAuthorizationHandler(SnapshotAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WorkspaceMemberRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return; // DECISION: no route context to verify against -> deny by default.
        }

        if (httpContext.Request.RouteValues["workspaceId"] is not string workspaceId || workspaceId.Length == 0)
        {
            return;
        }

        var userId = context.User.FindFirst(SnapshotClaimTypes.UserId)?.Value;
        var sid = context.User.FindFirst(SnapshotClaimTypes.SessionId)?.Value;
        if (userId is null || sid is null)
        {
            return;
        }

        var result = await _accessor.GetAsync(workspaceId, userId, sid, httpContext.RequestAborted);
        if (result.Snapshot is not null
            && !result.SessionRevoked
            && result.Snapshot.Version == PermissionEvaluator.SupportedSnapshotVersion)
        {
            context.Succeed(requirement);
        }
    }
}
