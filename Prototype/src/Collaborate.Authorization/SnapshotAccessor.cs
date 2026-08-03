using Collaborate.Authorization.Models;

namespace Collaborate.Authorization;

/// <summary>
/// Scoped per-request memoization in front of <see cref="ISnapshotStore"/>. Any
/// number of authorization checks in one request cost exactly one store round trip.
/// </summary>
public sealed class SnapshotAccessor
{
    private readonly ISnapshotStore _store;
    private (string WorkspaceId, string UserId, string Sid)? _key;
    private SnapshotResult? _result;

    public SnapshotAccessor(ISnapshotStore store)
    {
        _store = store;
    }

    public async Task<SnapshotResult> GetAsync(string workspaceId, string userId, string sid, CancellationToken ct = default)
    {
        if (_result is not null && _key == (workspaceId, userId, sid))
        {
            return _result;
        }

        _result = await _store.GetAsync(workspaceId, userId, sid, ct);
        _key = (workspaceId, userId, sid);
        return _result;
    }
}
