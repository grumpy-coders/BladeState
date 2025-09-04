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

    public virtual Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(State); // get whatever the current value is. unsure of consequences here :P

        return Task.FromResult(State ?? new T());
    }

    public virtual Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        State = state;
        return Task.CompletedTask;
    }

    public virtual Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
            
        State = new T();
        return Task.CompletedTask;
    }

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
