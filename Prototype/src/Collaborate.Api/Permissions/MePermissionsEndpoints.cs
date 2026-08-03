using System.Security.Claims;
using Collaborate.Authorization;
using Collaborate.Authorization.Models;

namespace Collaborate.Api.Permissions;

public static class MePermissionsEndpoints
{
    public static void MapMePermissionsEndpoints(this WebApplication app)
    {
        // Advisory only — see the README. Enforcement always re-reads at the resource;
        // this exists so callers can render UI or skip duplicating the predicate.
        app.MapGet("/workspaces/{workspaceId}/me/permissions", async (
                string workspaceId,
                ClaimsPrincipal user,
                SnapshotAccessor accessor,
                TimeProvider timeProvider,
                HttpContext httpContext) =>
            {
                var userId = user.FindFirst(SnapshotClaimTypes.UserId)?.Value;
                var sid = user.FindFirst(SnapshotClaimTypes.SessionId)?.Value;

                // The WorkspaceMember policy already required these claims and a live
                // snapshot to reach this handler; recomputing here would duplicate the
                // predicate, so treat their absence as an invariant, not a new decision.
                var snapshot = userId is not null && sid is not null
                    ? (await accessor.GetAsync(workspaceId, userId, sid, httpContext.RequestAborted)).Snapshot
                    : null;

                if (snapshot is null)
                {
                    return Results.Forbid();
                }

                var actions = PermissionEvaluator.BaselineActions(snapshot.Role)
                    .Where(action => !snapshot.FirmDenies.Contains(action))
                    .OrderBy(action => action, StringComparer.Ordinal)
                    .ToArray();

                var deniedResources = snapshot.Overrides
                    .Where(kv => kv.Value == ResourceDecision.Deny)
                    .Select(kv => kv.Key)
                    .ToArray();

                var grantedResources = snapshot.Overrides
                    .Where(kv => kv.Value == ResourceDecision.Allow)
                    .Select(kv => kv.Key)
                    .ToArray();

                var response = new MePermissionsResponse(
                    workspaceId,
                    snapshot.Role,
                    actions,
                    deniedResources,
                    grantedResources,
                    snapshot.Version,
                    timeProvider.GetUtcNow());

                httpContext.Response.Headers.CacheControl = "no-store";
                return Results.Ok(response);
            })
            .RequireAuthorization(AuthorizationPolicies.WorkspaceMember);
    }
}
