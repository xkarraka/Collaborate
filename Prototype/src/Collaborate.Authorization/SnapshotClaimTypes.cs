namespace Collaborate.Authorization;

/// <summary>Claim types the handlers read off the validated token. Assumes
/// <c>MapInboundClaims = false</c> on JwtBearerOptions so these match the token verbatim.</summary>
public static class SnapshotClaimTypes
{
    public const string UserId = "sub";

    public const string SessionId = "sid";
}
