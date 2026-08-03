using System.Text;
using Collaborate.Api.Documents;
using Collaborate.Api.Fakes;
using Collaborate.Api.Permissions;
using Collaborate.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
        };
    });

builder.Services.AddCollaborateAuthorization();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPermissionSource, FakePermissionSource>();
builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();

// DECISION: no Redis connection string configured -> fall back to a direct,
// uncached read of the fake source rather than requiring infra for a local run.
// Real deployments call AddRedisSnapshotStore with a connection string instead.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddRedisSnapshotStore(redisConnectionString);
}
else
{
    builder.Services.AddSingleton<ISnapshotStore, DirectSnapshotStore>();
}

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDocumentEndpoints();
app.MapMePermissionsEndpoints();

app.Run();

public partial class Program
{
}
