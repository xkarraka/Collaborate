using System.Text.Json.Serialization;

namespace Collaborate.Authorization.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SnapshotRole>))]
public enum SnapshotRole
{
    [JsonStringEnumMemberName("viewer")]
    Viewer,

    [JsonStringEnumMemberName("contributor")]
    Contributor,

    [JsonStringEnumMemberName("owner")]
    Owner
}
