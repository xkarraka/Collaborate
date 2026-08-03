using Collaborate.Authorization;
using Collaborate.Authorization.Models;

namespace Collaborate.Authorization.Tests.Predicate;

public class PermissionEvaluatorTests
{
    private static PermissionSnapshot Snapshot(
        SnapshotRole role,
        IReadOnlyDictionary<string, ResourceDecision>? overrides = null,
        IReadOnlyList<string>? firmDenies = null,
        int version = PermissionEvaluator.SupportedSnapshotVersion) => new()
    {
        Version = version,
        WorkspaceId = "ws_1",
        UserId = "usr_1",
        Role = role,
        Overrides = overrides ?? new Dictionary<string, ResourceDecision>(),
        FirmDenies = firmDenies ?? Array.Empty<string>(),
        BuiltAt = DateTimeOffset.UnixEpoch,
    };

    public static IEnumerable<object[]> RoleBaselineCases()
    {
        // Cross role x action against the role baseline table in the spec.
        yield return new object[] { SnapshotRole.Viewer, "read", true };
        yield return new object[] { SnapshotRole.Viewer, "comment", false };
        yield return new object[] { SnapshotRole.Viewer, "write", false };
        yield return new object[] { SnapshotRole.Viewer, "share", false };
        yield return new object[] { SnapshotRole.Viewer, "manage", false };
        yield return new object[] { SnapshotRole.Viewer, "export", false };

        yield return new object[] { SnapshotRole.Contributor, "read", true };
        yield return new object[] { SnapshotRole.Contributor, "comment", true };
        yield return new object[] { SnapshotRole.Contributor, "write", true };
        yield return new object[] { SnapshotRole.Contributor, "share", false };
        yield return new object[] { SnapshotRole.Contributor, "manage", false };
        yield return new object[] { SnapshotRole.Contributor, "export", false };

        yield return new object[] { SnapshotRole.Owner, "read", true };
        yield return new object[] { SnapshotRole.Owner, "comment", true };
        yield return new object[] { SnapshotRole.Owner, "write", true };
        yield return new object[] { SnapshotRole.Owner, "share", true };
        yield return new object[] { SnapshotRole.Owner, "manage", true };
        yield return new object[] { SnapshotRole.Owner, "export", true };
    }

    [Theory]
    [MemberData(nameof(RoleBaselineCases))]
    public void Role_baseline_actions_match_the_table(SnapshotRole role, string action, bool expectedAllowed)
    {
        var snapshot = Snapshot(role);

        var allowed = PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: false, action);

        Assert.Equal(expectedAllowed, allowed);
    }

    [Fact]
    public void Deny_override_beats_owner_role()
    {
        var snapshot = Snapshot(
            SnapshotRole.Owner,
            overrides: new Dictionary<string, ResourceDecision> { ["doc_1"] = ResourceDecision.Deny });

        Assert.False(PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: false, "read", "doc_1"));
    }

    [Fact]
    public void Allow_override_widens_viewer_role()
    {
        var snapshot = Snapshot(
            SnapshotRole.Viewer,
            overrides: new Dictionary<string, ResourceDecision> { ["doc_1"] = ResourceDecision.Allow });

        Assert.True(PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: false, "write", "doc_1"));
    }

    [Fact]
    public void Firm_deny_beats_owner_export_override_allow()
    {
        var snapshot = Snapshot(
            SnapshotRole.Owner,
            overrides: new Dictionary<string, ResourceDecision> { ["doc_1"] = ResourceDecision.Allow },
            firmDenies: new[] { "export" });

        Assert.False(PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: false, "export", "doc_1"));
    }

    [Fact]
    public void Deny_override_on_other_resource_does_not_affect_this_resource()
    {
        var snapshot = Snapshot(
            SnapshotRole.Owner,
            overrides: new Dictionary<string, ResourceDecision> { ["doc_1"] = ResourceDecision.Deny });

        Assert.True(PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: false, "read", "doc_2"));
    }

    [Fact]
    public void No_resource_id_skips_override_check()
    {
        var snapshot = Snapshot(
            SnapshotRole.Viewer,
            overrides: new Dictionary<string, ResourceDecision> { ["doc_1"] = ResourceDecision.Allow });

        Assert.False(PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: false, "write"));
    }

    [Fact]
    public void Null_snapshot_denies()
    {
        Assert.False(PermissionEvaluator.IsAllowed(null, sessionRevoked: false, "read"));
    }

    [Fact]
    public void Unknown_schema_version_denies()
    {
        var snapshot = Snapshot(SnapshotRole.Owner, version: 99);

        Assert.False(PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: false, "read"));
    }

    [Fact]
    public void Revoked_session_denies_even_for_owner()
    {
        var snapshot = Snapshot(SnapshotRole.Owner);

        Assert.False(PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: true, "read"));
    }

    [Fact]
    public void Revoked_session_denies_even_with_allow_override()
    {
        var snapshot = Snapshot(
            SnapshotRole.Viewer,
            overrides: new Dictionary<string, ResourceDecision> { ["doc_1"] = ResourceDecision.Allow });

        Assert.False(PermissionEvaluator.IsAllowed(snapshot, sessionRevoked: true, "write", "doc_1"));
    }
}
