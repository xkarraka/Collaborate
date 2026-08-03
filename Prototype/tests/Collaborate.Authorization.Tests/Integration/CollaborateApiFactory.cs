using Collaborate.Authorization;
using Collaborate.Authorization.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Collaborate.Authorization.Tests.Integration;

/// <summary>Swaps the API's default store for the call-counting in-memory fake so
/// tests can assert on store round trips without touching Redis.</summary>
public sealed class CollaborateApiFactory : WebApplicationFactory<Program>
{
    public InMemorySnapshotStore Store { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ISnapshotStore>();
            services.AddSingleton<ISnapshotStore>(Store);
        });
    }
}
