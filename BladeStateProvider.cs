using System;
using System.Threading.Tasks;

namespace BladeState;

/// <summary>
/// Defines persistence for a given state type.
/// </summary>
public abstract class BladeStateProvider<T> : IDisposable where T : class, new()
{
    public T State { get; set; } = new T();

    public Profile Profile { get; set; } = new();

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

    // --- IDisposable pattern ---
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // free managed resources here
                // subclasses can override to dispose e.g. DbContext, Redis connection, etc.
            }

            // free unmanaged resources here (if any)

            _disposed = true;
        }
    }
}
