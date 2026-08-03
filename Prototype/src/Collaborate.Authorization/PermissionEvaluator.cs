using Collaborate.Authorization.Models;

namespace Collaborate.Authorization;

/// <summary>
/// The single access predicate. Pure, no I/O, no clock — see the evaluation
/// order spec in the README before changing this. Every unknown state denies.
/// </summary>
public static class PermissionEvaluator
{
    public const int SupportedSnapshotVersion = 1;

    private static readonly IReadOnlyDictionary<SnapshotRole, IReadOnlySet<string>> RoleActions =
        new Dictionary<SnapshotRole, IReadOnlySet<string>>
        {
            [SnapshotRole.Viewer] = new HashSet<string> { "read" },
            [SnapshotRole.Contributor] = new HashSet<string> { "read", "comment", "write" },
            [SnapshotRole.Owner] = new HashSet<string> { "read", "comment", "write", "share", "manage", "export" },
        };

    public static IReadOnlySet<string> BaselineActions(SnapshotRole role) => RoleActions[role];

    public static bool IsAllowed(PermissionSnapshot? snapshot, bool sessionRevoked, string action, string? resourceId = null)
    {
        // Steps 1-2: unrecognised version or missing snapshot both deny; they are
        // mutually exclusive (a null snapshot has no version to recognise) so the
        // order between them cannot be observed.
        if (snapshot is null || snapshot.Version != SupportedSnapshotVersion)
        {
            return false;
        }

        // Step 3
        if (sessionRevoked)
        {
            return false;
        }

        // Step 4: firm policy outranks sharing decisions.
        if (snapshot.FirmDenies.Contains(action))
        {
            return false;
        }

        // Steps 5-6: an explicit deny beats an explicit allow because deny is checked first.
        if (resourceId is not null && snapshot.Overrides.TryGetValue(resourceId, out var decision))
        {
            if (decision == ResourceDecision.Deny)
            {
                return false;
            }

            if (decision == ResourceDecision.Allow)
            {
                return true;
            }
        }

        // Step 7
        if (RoleActions.TryGetValue(snapshot.Role, out var actions) && actions.Contains(action))
        {
            return true;
        }

        // Step 8
        return false;
    }
}
