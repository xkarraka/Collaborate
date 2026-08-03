using Collaborate.Authorization;
using Collaborate.Authorization.Models;

namespace Collaborate.Api.Fakes;

/// <summary>Local-dev fallback used when no Redis connection string is configured:
/// reads the fake source directly on every call, no cache layer. Production wiring
/// is <c>AddRedisSnapshotStore</c>; see the README for why this exists.</summary>
public sealed class DirectSnapshotStore : ISnapshotStore
{
    private static readonly HashSet<string> RevokedSessions = new() { "sid_revoked" };

    private readonly IPermissionSource _source;

    public DirectSnapshotStore(IPermissionSource source)
    {
        _source = source;
    }

    public async Task<SnapshotResult> GetAsync(string workspaceId, string userId, string sid, CancellationToken ct)
    {
        var snapshot = await _source.BuildSnapshotAsync(workspaceId, userId, ct);
        return new SnapshotResult(snapshot, RevokedSessions.Contains(sid));
    }
}
