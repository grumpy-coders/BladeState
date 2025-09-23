using Microsoft.Extensions.DependencyInjection;
using GrumpyCoders.BladeState.Cryptography;
using GrumpyCoders.BladeState.Models;
using GrumpyCoders.BladeState.Providers;
using System;
using System.Data.Common;
using GrumpyCoders.BladeState.Enums;
using Microsoft.EntityFrameworkCore;
using GrumpyCoders.BladeState.Data.EntityFrameworkCore;

namespace GrumpyCoders.BladeState;

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
    /// Registers a BladeState provider using SQL (direct ADO.NET or custom provider).
    /// Generic variant for strongly-typed state. Need to pass a connection delegate
    /// </summary>
    public static IServiceCollection AddSqlBladeState<TState>(
        this IServiceCollection services,
        Func<DbConnection> connectionDelegate,
        BladeStateProfile profile)
        where TState : class, new()
    {
        services.AddSingleton(profile);
        services.AddSingleton(new BladeStateCryptography(profile.EncryptionKey));
        services.AddSingleton(sp => new SqlBladeStateProvider<TState>(connectionDelegate, sp.GetRequiredService<BladeStateCryptography>(), sp.GetRequiredService<BladeStateProfile>()));

        return services;
    }

    public static IServiceCollection AddEfCoreBladeState<TState>(
                this IServiceCollection services,
                BladeStateProfile profile,
                Action<DbContextOptionsBuilder> optionsAction)
                where TState : class, new()
    {
        // Profile + crypto = safe as singleton
        services.AddSingleton(profile);
        services.AddSingleton(sp => new BladeStateCryptography(profile.EncryptionKey));

        // Use DbContextFactory so providers always get a fresh context
        services.AddDbContextFactory<BladeStateDbContext>(optionsAction);

        // Provider depends on DbContextFactory (not DbContext directly)
        services.AddScoped(sp =>
            new EfCoreBladeStateProvider<TState>(
                sp.GetRequiredService<IDbContextFactory<BladeStateDbContext>>(),
                sp.GetRequiredService<BladeStateCryptography>(),
                sp.GetRequiredService<BladeStateProfile>()));

        return services;
    }
}