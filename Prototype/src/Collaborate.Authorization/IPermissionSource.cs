using Collaborate.Authorization.Models;

namespace Collaborate.Authorization;

/// <summary>
/// The system of record behind the cache. Faked at this boundary — a real
/// implementation would read from wherever roles/overrides/firm denies are authored.
/// </summary>
public interface IPermissionSource
{
    Task<PermissionSnapshot?> BuildSnapshotAsync(string workspaceId, string userId, CancellationToken ct);
}
