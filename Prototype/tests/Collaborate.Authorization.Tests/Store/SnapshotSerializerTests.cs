using Collaborate.Authorization;
using Collaborate.Authorization.Models;

namespace Collaborate.Authorization.Tests.Store;

public class SnapshotSerializerTests
{
    [Fact]
    public void Round_trips_a_snapshot()
    {
        var snapshot = new PermissionSnapshot
        {
            Version = 1,
            WorkspaceId = "ws_88213",
            UserId = "usr_4d9f",
            Role = SnapshotRole.Contributor,
            Overrides = new Dictionary<string, ResourceDecision>
            {
                ["doc_5512"] = ResourceDecision.Deny,
                ["doc_9001"] = ResourceDecision.Allow,
            },
            FirmDenies = new[] { "export" },
            BuiltAt = DateTimeOffset.Parse("2026-08-02T10:00:00Z"),
        };

        var json = SnapshotSerializer.Serialize(snapshot);
        var roundTripped = SnapshotSerializer.TryDeserialize(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(snapshot.Version, roundTripped.Version);
        Assert.Equal(snapshot.WorkspaceId, roundTripped.WorkspaceId);
        Assert.Equal(snapshot.UserId, roundTripped.UserId);
        Assert.Equal(snapshot.Role, roundTripped.Role);
        Assert.Equal(snapshot.Overrides, roundTripped.Overrides);
        Assert.Equal(snapshot.FirmDenies, roundTripped.FirmDenies);
        Assert.Equal(snapshot.BuiltAt, roundTripped.BuiltAt);
    }

    [Theory]
    [InlineData("{ not valid json")]
    [InlineData("")]
    [InlineData("{\"v\": 1, \"role\": \"admin\"}")]
    public void Malformed_content_deserializes_to_null(string malformedJson)
    {
        Assert.Null(SnapshotSerializer.TryDeserialize(malformedJson));
    }
}
