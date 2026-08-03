namespace Collaborate.Authorization;

public sealed class RedisSnapshotStoreOptions
{
    /// <summary>How long a populated snapshot cache entry lives before the next request re-reads the source.</summary>
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromSeconds(30);
}
