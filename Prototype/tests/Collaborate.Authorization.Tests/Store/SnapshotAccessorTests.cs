using Collaborate.Authorization;
using Collaborate.Authorization.Models;
using Collaborate.Authorization.Testing;

namespace Collaborate.Authorization.Tests.Store;

public class SnapshotAccessorTests
{
    [Fact]
    public async Task Fifty_checks_in_one_request_cost_exactly_one_store_call()
    {
        var store = new InMemorySnapshotStore();
        store.SetSnapshot("ws_1", "usr_1", new PermissionSnapshot
        {
            Version = PermissionEvaluator.SupportedSnapshotVersion,
            WorkspaceId = "ws_1",
            UserId = "usr_1",
            Role = SnapshotRole.Owner,
            BuiltAt = DateTimeOffset.UnixEpoch,
        });

        var accessor = new SnapshotAccessor(store);

        for (var i = 0; i < 50; i++)
        {
            var result = await accessor.GetAsync("ws_1", "usr_1", "sid_1");
            Assert.NotNull(result.Snapshot);
        }

        Assert.Equal(1, store.CallCount);
    }

    [Fact]
    public async Task Different_keys_within_a_request_each_cost_a_store_call()
    {
        var store = new InMemorySnapshotStore();
        var accessor = new SnapshotAccessor(store);

        await accessor.GetAsync("ws_1", "usr_1", "sid_1");
        await accessor.GetAsync("ws_2", "usr_1", "sid_1");

        Assert.Equal(2, store.CallCount);
    }
}
