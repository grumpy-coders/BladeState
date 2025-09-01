using Microsoft.Extensions.DependencyInjection;

namespace BladeState;

public static class BladeStateServiceCollectionExtensions
{
    public static IServiceCollection AddBladeState<T, TProvider>(this IServiceCollection services)
        where T : class, new()
        where TProvider : class, IBladeStateProvider<T>
    {
        services.AddSingleton<IBladeStateProvider<T>, TProvider>();
        services.AddSingleton<BladeStateStore<T>>();
        return services;
    }
}
