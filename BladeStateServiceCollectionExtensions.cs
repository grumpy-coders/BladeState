using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BladeState.Cryptography;
using BladeState.Models;

namespace BladeState;

public static class BladeStateServiceCollectionExtensions
{
    public static IServiceCollection AddBladeState<T, TProvider>(
        this IServiceCollection services)
        where T : class, new()
        where TProvider : BladeStateProvider<T>
    {
        // Configure Profile when the service provider is built
        services.AddOptions<Profile>()
            .Configure<IConfiguration>((profile, config) =>
            {
                config.GetSection("BladeState:Profile").Bind(profile);
            });

        services.AddSingleton<BladeStateProvider<T>, TProvider>();

        // Register cryptography service depending on Profile
        services.AddSingleton(sp =>
        {
            Profile profile = sp.GetRequiredService<IOptions<Profile>>().Value;
            return new BladeStateCryptography(profile.EncryptionKey);
        });

        return services;
    }
}
