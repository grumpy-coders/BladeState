using Microsoft.Extensions.DependencyInjection;
using BladeState.Cryptography;
using BladeState.Models;

namespace BladeState;

public static class BladeStateServiceCollectionExtensions
{
    public static IServiceCollection AddBladeState<T>(
        this IServiceCollection services,
        BladeStateProfile profile)
        where T : class, new()
    {
        services.AddSingleton(profile);

        services.AddSingleton(new BladeStateCryptography(profile.EncryptionKey));

        services.AddSingleton<BladeStateProvider<T>>();

        return services;
    }
}