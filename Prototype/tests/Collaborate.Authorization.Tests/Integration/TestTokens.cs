using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Collaborate.Authorization.Tests.Integration;

/// <summary>Mints HS256 tokens with the same signing key/issuer/audience the API
/// trusts (see Collaborate.Api/appsettings.json) — no real IdP involved.</summary>
public static class TestTokens
{
    public const string SigningKey = "dev-only-test-signing-key-do-not-use-in-production-32bytes-min";
    public const string Issuer = "https://collaborate.test";
    public const string Audience = "collaborate-api";

    public static string Create(string userId, string sid, TimeSpan? lifetime = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim("sid", sid),
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(5)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
