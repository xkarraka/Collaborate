using System.Text.Json.Serialization;

namespace Collaborate.Authorization.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ResourceDecision>))]
public enum ResourceDecision
{
    [JsonStringEnumMemberName("deny")]
    Deny,

    [JsonStringEnumMemberName("allow")]
    Allow
}
