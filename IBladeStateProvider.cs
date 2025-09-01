using System.Threading.Tasks;

namespace BladeState;

/// <summary>
/// Defines persistence for a given state type.
/// </summary>
public interface IBladeStateProvider<T> where T : class, new()
{
    Task<T> LoadStateAsync();
    Task SaveStateAsync(T state);
    Task ClearStateAsync();
}
