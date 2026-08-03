using Collaborate.Authorization.Models;

namespace Collaborate.Authorization;

public interface ISnapshotStore
{
    Task<SnapshotResult> GetAsync(string workspaceId, string userId, string sid, CancellationToken ct);
}
