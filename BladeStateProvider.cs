using System;
using System.Threading.Tasks;

namespace BladeState;

/// <summary>
/// Defines persistence for a given state type.
/// </summary>
public abstract class BladeStateProvider<T> where T : class, new()
{
    public T State { get; set; } = new T();
    public virtual Task<T> LoadStateAsync()
    {
        return Task.FromResult(State ?? new T());
    }

    public virtual Task SaveStateAsync(T state)
    {
        State = state;
        return Task.CompletedTask;
    }

    public virtual Task ClearStateAsync()
    {
        State = new T();
        return Task.CompletedTask;
    }

    public Profile Profile { get; set; } = new();
}
