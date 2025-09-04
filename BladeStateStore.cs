using System.Threading.Tasks;

namespace BladeState;

public class BladeStateStore<T>(BladeStateProvider<T> provider) where T : class, new()
{
    private readonly BladeStateProvider<T> _provider = provider;

    public T State
    {
        get => _provider.State ?? new T();
        set => _provider.State = value;
    }

    public async Task LoadStateAsync()
    {
        T databaseState = await _provider.LoadStateAsync();
        State = databaseState ?? new T();
    }

    public async Task SaveStateAsync(T state)
    {
        _provider.State = state;
        await _provider.SaveStateAsync(state);
    }

    public async Task ClearStateAsync()
    {
        State = new T();
        await _provider.ClearStateAsync();
    }
}
