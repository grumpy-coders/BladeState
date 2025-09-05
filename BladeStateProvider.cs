using System;
using System.Threading;
using System.Threading.Tasks;

namespace BladeState;

/// <summary>
/// Defines persistence for a given state type.
/// </summary>
public abstract class BladeStateProvider<T> : IAsyncDisposable where T : class, new()
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

	public async ValueTask DisposeAsync()
	{
		if (!_disposed)
		{
			// Let subclasses override async cleanup
			await DisposeAsyncCore();

			// suppress finalizer
			GC.SuppressFinalize(this);
			_disposed = true;
		}
	}

	/// <summary>
	/// Subclasses override this to dispose async resources
	/// (e.g., DbContext.DisposeAsync, Redis connection, etc.)
	/// </summary>
	protected virtual ValueTask DisposeAsyncCore()
	{
		// Default: nothing async to dispose
		return ValueTask.CompletedTask;
	}

}
