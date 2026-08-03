using Collaborate.Authorization.Models;

namespace Collaborate.Authorization.Testing;

/// <summary>Test fake for <see cref="ISnapshotStore"/>. Counts calls so accessor
/// memoization can be asserted without standing up Redis.</summary>
public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly Dictionary<(string WorkspaceId, string UserId), PermissionSnapshot> _snapshots = new();
    private readonly HashSet<string> _revokedSessions = new();

    public int CallCount { get; private set; }

    public void SetSnapshot(string workspaceId, string userId, PermissionSnapshot snapshot) =>
        _snapshots[(workspaceId, userId)] = snapshot;

    public void RevokeSession(string sid) => _revokedSessions.Add(sid);

    public Task<SnapshotResult> GetAsync(string workspaceId, string userId, string sid, CancellationToken ct)
    {
        CallCount++;

        _snapshots.TryGetValue((workspaceId, userId), out var snapshot);
        var revoked = _revokedSessions.Contains(sid);

        return Task.FromResult(new SnapshotResult(snapshot, revoked));
    }
}
