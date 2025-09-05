using Microsoft.Extensions.DependencyInjection;
using BladeState.Cryptography;

namespace BladeState;

public static class BladeStateServiceCollectionExtensions
{
    public static IServiceCollection AddBladeState<T, TProvider>(
        this IServiceCollection services,
        bool useEncryption = true,
        string encryptionKey = "")
        where T : class, new()
        where TProvider : BladeStateProvider<T>
    {
        services.AddSingleton<BladeStateProvider<T>, TProvider>();

        if (useEncryption)
        {
            services.AddSingleton(_ => new BladeStateCryptography(encryptionKey));
        }

        return services;
    }
}