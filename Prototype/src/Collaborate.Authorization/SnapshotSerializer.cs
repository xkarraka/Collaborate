using System.Text.Json;
using Collaborate.Authorization.Models;

namespace Collaborate.Authorization;

/// <summary>Isolates (de)serialization so malformed cache content can be tested
/// without a live Redis connection.</summary>
internal static class SnapshotSerializer
{
    public static string Serialize(PermissionSnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, PermissionsJsonContext.Default.PermissionSnapshot);

    /// <summary>Returns null on any malformed input — the caller treats that the
    /// same as a missing snapshot, which the predicate denies.</summary>
    public static PermissionSnapshot? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(json, PermissionsJsonContext.Default.PermissionSnapshot);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
