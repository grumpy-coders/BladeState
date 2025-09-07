using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
using BladeState.Models;

namespace BladeState;

/// <summary>
/// Defines persistence for a given state type.
/// </summary>
public abstract class BladeStateProvider<T>(BladeStateCryptography bladeStateCryptography) : IAsyncDisposable where T : class, new()
{
    protected readonly BladeStateCryptography BladeStateCryptography = bladeStateCryptography;
    protected T State { get; set; } = new T();
	protected string CipherState { get; set; } = string.Empty;
    protected Profile Profile { get; set; } = new();

    public virtual Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(State);

        return Task.FromResult(State ?? new T());
    }

    public virtual Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        State = state;

		if (AutoEncrypt)
		{
			EncryptState();
		}

		return Task.CompletedTask;
    }

    public virtual Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

		State = new T();
		CipherState = string.Empty;

        return Task.CompletedTask;
    }

    public virtual void EncryptState()
    {
        CipherState = BladeStateCryptography.Encrypt(JsonSerializer.Serialize(State));
    }

    public virtual void DecryptState()
    {
        State = JsonSerializer.Deserialize<T>(BladeStateCryptography.Decrypt(CipherState));        
    }

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }

    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
