using Microsoft.Extensions.DependencyInjection;
using BladeState.Cryptography;
using BladeState.Models;
using BladeState.Providers;
using System;
using System.Data.Common;
using BladeState.Enums;
using Microsoft.EntityFrameworkCore;
using BladeState.Data.EntityFrameworkCore;

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
        BladeStateProfile profile,
        SqlType sqlType = SqlType.SqlServer)
        where TState : class, new()
    {
        services.AddSingleton(profile);
        services.AddSingleton(new BladeStateCryptography(profile.EncryptionKey));
        services.AddSingleton(sp => new SqlBladeStateProvider<TState>(connectionDelegate, sp.GetRequiredService<BladeStateCryptography>(), sp.GetRequiredService<BladeStateProfile>(), sqlType));

        return services;
    }

    public static IServiceCollection AddEfCoreBladeState<TState>(
        this IServiceCollection services,
        BladeStateProfile profile,
        Action<DbContextOptionsBuilder> optionsAction)
        where TState : class, new()
    {
        services.AddSingleton(profile);
        services.AddSingleton(sp => new BladeStateCryptography(profile.EncryptionKey));

        services.AddDbContextFactory<BladeStateDbContext>(
            optionsAction,
            ServiceLifetime.Scoped
        );

        services.AddScoped(sp =>
            new EfCoreBladeStateProvider<TState>(
                sp.GetRequiredService<IDbContextFactory<BladeStateDbContext>>(),
                sp.GetRequiredService<BladeStateCryptography>(),
                sp.GetRequiredService<BladeStateProfile>()));


        return services;
    }
}