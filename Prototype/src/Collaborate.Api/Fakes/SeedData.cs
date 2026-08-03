using Collaborate.Authorization;
using Collaborate.Authorization.Models;
using Collaborate.Api.Documents;

namespace Collaborate.Api.Fakes;

/// <summary>Canned data for the sample resource service. Stands in for whatever
/// real system would author roles/overrides and store documents.</summary>
public static class SeedData
{
    public const string WorkspaceId = "ws_88213";

    public static readonly IReadOnlyDictionary<string, PermissionSnapshot> Snapshots = new Dictionary<string, PermissionSnapshot>
    {
        ["usr_4d9f"] = new PermissionSnapshot
        {
            Version = PermissionEvaluator.SupportedSnapshotVersion,
            WorkspaceId = WorkspaceId,
            UserId = "usr_4d9f",
            Role = SnapshotRole.Contributor,
            Overrides = new Dictionary<string, ResourceDecision>
            {
                ["doc_5512"] = ResourceDecision.Deny,
                ["doc_9001"] = ResourceDecision.Allow,
            },
            FirmDenies = new[] { "export" },
            BuiltAt = DateTimeOffset.Parse("2026-08-02T10:00:00Z"),
        },
        ["usr_owner"] = new PermissionSnapshot
        {
            Version = PermissionEvaluator.SupportedSnapshotVersion,
            WorkspaceId = WorkspaceId,
            UserId = "usr_owner",
            Role = SnapshotRole.Owner,
            FirmDenies = new[] { "export" },
            BuiltAt = DateTimeOffset.Parse("2026-08-02T10:00:00Z"),
        },
        ["usr_viewer"] = new PermissionSnapshot
        {
            Version = PermissionEvaluator.SupportedSnapshotVersion,
            WorkspaceId = WorkspaceId,
            UserId = "usr_viewer",
            Role = SnapshotRole.Viewer,
            BuiltAt = DateTimeOffset.Parse("2026-08-02T10:00:00Z"),
        },
    };

    public static readonly IReadOnlyList<DocumentRecord> Documents = new[]
    {
        new DocumentRecord("doc_5512", WorkspaceId, "Q3 roadmap"),
        new DocumentRecord("doc_9001", WorkspaceId, "Compliance notes"),
        new DocumentRecord("doc_1000", WorkspaceId, "Team handbook"),
    };
}
