using System.Threading.Tasks;

namespace BladeState.Providers;

public class InMemoryBladeStateProvider<T> : IBladeStateProvider<T> where T : class, new()
{
    private T _state;

    public Task<T> LoadStateAsync()
    {
        return Task.FromResult(_state ?? new T());
    }

    public Task SaveStateAsync(T state)
    {
        _state = state;
        return Task.CompletedTask;
    }

    public Task ClearStateAsync()
    {
        _state = new T();
        return Task.CompletedTask;
    }
}
