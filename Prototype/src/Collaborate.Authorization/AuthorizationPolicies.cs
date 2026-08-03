namespace Collaborate.Authorization;

public static class AuthorizationPolicies
{
    /// <summary>Coarse, route-level check: does this user have a live, non-revoked snapshot for the route's workspace?</summary>
    public const string WorkspaceMember = "WorkspaceMember";
}
