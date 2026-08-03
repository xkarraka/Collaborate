using Collaborate.Authorization;
using Collaborate.Authorization.Models;

namespace Collaborate.Api.Fakes;

/// <summary>Fake system of record behind the cache — a real implementation
/// would read from wherever roles/overrides/firm denies are authored.</summary>
public sealed class FakePermissionSource : IPermissionSource
{
    public Task<PermissionSnapshot?> BuildSnapshotAsync(string workspaceId, string userId, CancellationToken ct)
    {
        if (workspaceId == SeedData.WorkspaceId && SeedData.Snapshots.TryGetValue(userId, out var snapshot))
        {
            return Task.FromResult<PermissionSnapshot?>(snapshot);
        }

        return Task.FromResult<PermissionSnapshot?>(null);
    }
}
