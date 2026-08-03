namespace Collaborate.Authorization.Models;

public sealed record SnapshotResult(PermissionSnapshot? Snapshot, bool SessionRevoked);
