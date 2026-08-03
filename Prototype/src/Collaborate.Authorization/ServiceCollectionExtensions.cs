using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Collaborate.Authorization;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the accessor, the two handlers, and the <see cref="AuthorizationPolicies.WorkspaceMember"/>
    /// policy. Does not register an <see cref="ISnapshotStore"/> — call <see cref="AddRedisSnapshotStore"/>
    /// or register a test fake yourself.</summary>
    public static IServiceCollection AddCollaborateAuthorization(this IServiceCollection services)
    {
        services.AddScoped<SnapshotAccessor>();
        services.AddScoped<IAuthorizationHandler, WorkspaceMemberAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ResourceActionAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.WorkspaceMember, policy => policy.Requirements.Add(new WorkspaceMemberRequirement()));

        return services;
    }

    /// <summary>Registers <see cref="RedisSnapshotStore"/> as the <see cref="ISnapshotStore"/>, backed by a
    /// singleton <see cref="IConnectionMultiplexer"/>. An <see cref="IPermissionSource"/> must be registered separately.</summary>
    public static IServiceCollection AddRedisSnapshotStore(
        this IServiceCollection services,
        string connectionString,
        Action<RedisSnapshotStoreOptions>? configureOptions = null)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
        services.AddOptions<RedisSnapshotStoreOptions>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddScoped<ISnapshotStore, RedisSnapshotStore>();

        return services;
    }
}
