using System.Threading.Tasks;

namespace BladeState;

public class BladeStateStore<T> where T : class, new()
{
    private readonly IBladeStateProvider<T> _provider;
    private T _state = new();

    public BladeStateStore(IBladeStateProvider<T> provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Gets the current state. If not loaded, it queries the provider or creates a new one.
    /// </summary>
    public T State => _state;

    public async Task LoadStateAsync()
    {
        var databaseState = await _provider.LoadStateAsync();
        _state = databaseState ?? new T();
    }

    public async Task SaveStateAsync(T state)
    {
        _state = state;
        await _provider.SaveStateAsync(state);
    }

    public async Task ClearStateAsync()
    {
        _state = new T();
        await _provider.ClearStateAsync();
    }
}
