using Microsoft.Extensions.DependencyInjection;
using BladeState.Cryptography;
using BladeState.Models;
using BladeState.Providers;

namespace BladeState;

public static class BladeStateServiceCollectionExtensions
{
    public static IServiceCollection AddBladeState<TState, TProvider>(
        this IServiceCollection services,
        BladeStateProfile profile)
        where TState : class, new()
        where TProvider : BladeStateProvider<TState>
    {
        services.AddSingleton(profile);
        services.AddSingleton(new BladeStateCryptography(profile.EncryptionKey));
        services.AddSingleton<TProvider>();

        return services;
    }

    /// <summary>
    /// Registers a BladeState provider using IMemoryCache as the backing store.
    /// </summary>
    public static IServiceCollection AddMemoryCacheBladeState<TState>(
        this IServiceCollection services,
        BladeStateProfile profile)
        where TState : class, new()
    {
        services.AddMemoryCache();
        services.AddSingleton(profile);
        services.AddSingleton(new BladeStateCryptography(profile.EncryptionKey));
        services.AddSingleton<MemoryCacheBladeStateProvider<TState>>();

        return services;
    }

    /// <summary>
    /// Registers a BladeState provider using Redis as the backing store.
    /// Requires that RedisBladeStateProvider<TState> exists.
    /// </summary>
    public static IServiceCollection AddRedisBladeState<TState>(
        this IServiceCollection services,
        BladeStateProfile profile)
        where TState : class, new()
    {
        services.AddSingleton(profile);
        services.AddSingleton(new BladeStateCryptography(profile.EncryptionKey));
        services.AddSingleton<RedisBladeStateProvider<TState>>();

        return services;
    }

    /// <summary>
    /// Registers a BladeState provider using Entity Framework Core (SQL) as the backing store.
    /// Requires that EfCoreBladeStateProvider<TState> exists.
    /// </summary>
    public static IServiceCollection AddEfCoreBladeState<TState>(
        this IServiceCollection services,
        BladeStateProfile profile)
        where TState : class, new()
    {
        services.AddSingleton(profile);
        services.AddSingleton(new BladeStateCryptography(profile.EncryptionKey));
        services.AddSingleton<EfCoreBladeStateProvider<TState>>();

        return services;
    }
}
