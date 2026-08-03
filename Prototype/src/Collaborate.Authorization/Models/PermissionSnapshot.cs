using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Collaborate.Authorization.Models;

public sealed record PermissionSnapshot
{
    [JsonPropertyName("v")]
    public required int Version { get; init; }

    [JsonPropertyName("workspaceId")]
    public required string WorkspaceId { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }

    [JsonPropertyName("role")]
    public required SnapshotRole Role { get; init; }

    [JsonPropertyName("overrides")]
    public IReadOnlyDictionary<string, ResourceDecision> Overrides { get; init; } =
        ImmutableDictionary<string, ResourceDecision>.Empty;

    [JsonPropertyName("firmDenies")]
    public IReadOnlyList<string> FirmDenies { get; init; } = Array.Empty<string>();

    [JsonPropertyName("builtAt")]
    public required DateTimeOffset BuiltAt { get; init; }
}
