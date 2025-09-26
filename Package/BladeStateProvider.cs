using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GrumpyCoders.BladeState.Cryptography;
using GrumpyCoders.BladeState.Enums;
using GrumpyCoders.BladeState.Events;
using GrumpyCoders.BladeState.Models;

namespace GrumpyCoders.BladeState.Providers;

/// <summary>
/// Defines persistence for a given state type with async timeout handling.
/// </summary>
public abstract class BladeStateProvider<T>(BladeStateCryptography bladeStateCryptography, BladeStateProfile bladeStateProfile)
	: IAsyncDisposable where T : class, new()
{
	protected readonly BladeStateCryptography Cryptography = bladeStateCryptography;
	protected readonly BladeStateProfile Profile = bladeStateProfile;
	protected T State { get; set; } = new T();

	protected DateTime LastAccessTime;
	private bool _disposed;

	public event EventHandler<BladeStateProviderEventArgs<T>> StateChanged = delegate { };

	/// <summary>
	/// Fires when a change occurs to the State. Such as after the State is loaded, saved, cleared. Can be used to update components, screens, force actions to occur, etc.
	/// </summary>
	/// <param name="eventType">The ProviderEventType to denote the type of change to the state</param>
	/// <returns></returns>
	protected virtual void OnStateChange(ProviderEventType eventType = ProviderEventType.None) => StateChanged(this, new BladeStateProviderEventArgs<T>(Profile.InstanceId, State, eventType));


	public virtual Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromResult(State);
		}
		State = new T();
		OnStateChange(ProviderEventType.Load);
		return Task.FromResult(State);
	}

	public virtual Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.CompletedTask;
		}

		State = state;
		LastAccessTime = DateTime.UtcNow;

		// Hash check to happen here to avoid unnecessary writes - card is coming

		try
		{
			if (Profile.AutoEncrypt)
			{
				EncryptState();
			}
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException("Could not encrypt state.", exception);
		}

		OnStateChange(ProviderEventType.Save);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Clears the current state.
	/// </summary>
	/// <param name="cancellationToken"></param>
	public virtual Task ClearStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.CompletedTask;
		}
		State = new T();
		OnStateChange(ProviderEventType.Clear);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Encrypts the current state into the CipherState property.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <exception cref="InvalidOperationException"></exception>
	public string EncryptState()
	{
		try
		{
			return Cryptography.Encrypt(JsonSerializer.Serialize(State));
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException($"Could not encrypt state. Ex: {exception.Message}");
		}
	}

	/// <summary>
	/// Decrypts the current CipherState into the State property.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public virtual string Decrypt(string cipherText)
	{
		try
		{
			return Cryptography.Decrypt(cipherText);
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException($"Could not decrypt state. Ex: {exception.Message}");
		}
	}

	/// <summary>
	/// Checks if the instance has timed out and clears the state if so.
	/// Updates the LastAccessTime to the current time.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	protected async Task<bool> CheckTimeoutAsync(CancellationToken cancellationToken = default)
	{

		if (cancellationToken.IsCancellationRequested)
		{
			return false;
		}

		TimeSpan delay = Profile.InstanceTimeout;
		TimeSpan elapsed = DateTime.UtcNow - LastAccessTime;
		TimeSpan remaining = delay - elapsed;
		if (remaining <= TimeSpan.Zero)
		{
			await ClearStateAsync(cancellationToken);
			return true;
		}
		return false;
	}

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