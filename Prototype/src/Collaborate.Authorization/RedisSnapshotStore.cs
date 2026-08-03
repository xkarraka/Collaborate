using Collaborate.Authorization.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Collaborate.Authorization;

/// <summary>
/// Cache-aside snapshot store. One MGET reads both the snapshot and the
/// revocation flag for a request; a miss falls through to <see cref="IPermissionSource"/>.
/// </summary>
public sealed class RedisSnapshotStore : ISnapshotStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IPermissionSource _source;
    private readonly RedisSnapshotStoreOptions _options;

    public RedisSnapshotStore(IConnectionMultiplexer redis, IPermissionSource source, IOptions<RedisSnapshotStoreOptions> options)
    {
        _redis = redis;
        _source = source;
        _options = options.Value;
    }

    public async Task<SnapshotResult> GetAsync(string workspaceId, string userId, string sid, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var snapshotKey = SnapshotKey(workspaceId, userId);
        var revocationKey = RevocationKey(sid);

        var values = await db.StringGetAsync(new RedisKey[] { snapshotKey, revocationKey });
        var sessionRevoked = values[1].HasValue;

        if (values[0].IsNullOrEmpty)
        {
            var built = await _source.BuildSnapshotAsync(workspaceId, userId, ct);
            if (built is null)
            {
                return new SnapshotResult(null, sessionRevoked);
            }

            var json = SnapshotSerializer.Serialize(built);
            await db.StringSetAsync(snapshotKey, json, _options.CacheDuration);
            return new SnapshotResult(built, sessionRevoked);
        }

        // A malformed cache entry fails closed rather than throwing.
        var snapshot = SnapshotSerializer.TryDeserialize(values[0]!);

        return new SnapshotResult(snapshot, sessionRevoked);
    }

    internal static string SnapshotKey(string workspaceId, string userId) => $"snap:{workspaceId}:{userId}";

    internal static string RevocationKey(string sid) => $"revoked:sid:{sid}";
}
