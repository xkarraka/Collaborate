using System.Text.Json.Serialization;

namespace Collaborate.Authorization.Models;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(PermissionSnapshot))]
public partial class PermissionsJsonContext : JsonSerializerContext
{
}
