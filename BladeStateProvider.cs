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
public abstract class BladeStateProvider<T>(BladeStateCryptography bladeStateCryptography, BladeStateProfile bladeStateProfile) : IAsyncDisposable where T : class, new()
{
	protected readonly BladeStateCryptography Cryptography = bladeStateCryptography;
	protected readonly BladeStateProfile Profile = bladeStateProfile;
	protected T State { get; set; } = new T();
	protected string CipherState { get; set; } = string.Empty;

	public virtual Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromResult(State);
		}

		if (Profile.AutoEncrypt)
		{
			DecryptState();
			return Task.FromResult(State);
		}

		return Task.FromResult(State ?? new T());
	}

	public virtual Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled(cancellationToken);

		State = state;

		if (Profile.AutoEncrypt)
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
		CipherState = Cryptography.Encrypt(JsonSerializer.Serialize(State));
	}

	public virtual void DecryptState()
	{
		State = JsonSerializer.Deserialize<T>(Cryptography.Decrypt(CipherState));
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
