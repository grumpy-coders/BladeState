using Microsoft.Extensions.DependencyInjection;
using BladeState.Cryptography;
using BladeState.Models;

namespace BladeState;

public static class BladeStateServiceCollectionExtensions
{
    public static IServiceCollection AddBladeState<T, TProvider>(
        this IServiceCollection services,
        BladeStateProfile profile)
        where T : class, new()
        where TProvider : BladeStateProvider<T>
    {
        services.AddSingleton(profile);

        services.AddSingleton(new BladeStateCryptography(profile.EncryptionKey));

        services.AddSingleton<BladeStateProvider<T>, TProvider>();

        return services;
    }
}