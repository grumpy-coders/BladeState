using System;
using System.Threading;
using System.Threading.Tasks;

namespace BladeState;

/// <summary>
/// Defines persistence for a given state type.
/// </summary>
public abstract class BladeStateProvider<T> : IDisposable where T : class, new()
{
    public T State { get; set; } = new T();

    public Profile Profile { get; set; } = new();

    // --- Load ---
    public Task<T> LoadStateAsync() =>
        LoadStateAsync(CancellationToken.None);

    public virtual Task<T> LoadStateAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(State ?? new T());
    }

    // --- Save ---
    public Task SaveStateAsync(T state) =>
        SaveStateAsync(state, CancellationToken.None);

    public virtual Task SaveStateAsync(T state, CancellationToken cancellationToken)
    {
        State = state;
        return Task.CompletedTask;
    }

    // --- Clear ---
    public Task ClearStateAsync() =>
        ClearStateAsync(CancellationToken.None);

    public virtual Task ClearStateAsync(CancellationToken cancellationToken)
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

            _disposed = true;
        }
    }
}
