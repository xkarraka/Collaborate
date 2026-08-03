using System.Text.Json.Serialization;
using Collaborate.Authorization.Models;

namespace Collaborate.Api.Permissions;

public sealed record MePermissionsResponse(
    [property: JsonPropertyName("workspaceId")] string WorkspaceId,
    [property: JsonPropertyName("role")] SnapshotRole Role,
    [property: JsonPropertyName("actions")] IReadOnlyList<string> Actions,
    [property: JsonPropertyName("deniedResources")] IReadOnlyList<string> DeniedResources,
    [property: JsonPropertyName("grantedResources")] IReadOnlyList<string> GrantedResources,
    [property: JsonPropertyName("snapshotVersion")] int SnapshotVersion,
    [property: JsonPropertyName("evaluatedAt")] DateTimeOffset EvaluatedAt);
