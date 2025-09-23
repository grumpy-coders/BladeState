using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GrumpyCoders.BladeState.Cryptography;
using GrumpyCoders.BladeState.Enums;
using GrumpyCoders.BladeState.Events;
using GrumpyCoders.BladeState.Models;

namespace GrumpyCoders.BladeState;

/// <summary>
/// Defines persistence for a given state type with async timeout handling.
/// </summary>
public abstract class BladeStateProvider<T>(BladeStateCryptography bladeStateCryptography, BladeStateProfile bladeStateProfile)
	: IAsyncDisposable where T : class, new()
{
	protected readonly BladeStateCryptography Cryptography = bladeStateCryptography;
	protected readonly BladeStateProfile Profile = bladeStateProfile;
	protected T State { get; set; } = new T();
	protected string CipherState { get; set; } = string.Empty;

	protected DateTime LastAccessTime;
	private bool _disposed;

	public event EventHandler<BladeStateProviderEventArgs<T>> StateChanged = delegate { };

	/// <summary>
	/// Fires when a change occurs to the State. Such as after the State is loaded, saved, cleared. Can be used to update components, screens, force actions to occur, etc.
	/// </summary>
	/// <param name="eventType">The ProviderEventType to denote the type of change to the state</param>
	/// <returns></returns>
	protected virtual void OnStateChange(ProviderEventType eventType = ProviderEventType.None) => StateChanged(this, new BladeStateProviderEventArgs<T>(Profile.InstanceId, State, eventType));


	public virtual async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return State;
		}

		await CheckTimeoutAsync(cancellationToken);

		try
		{
			if (Profile.AutoEncrypt)
			{
				await DecryptStateAsync(cancellationToken);
			}
		}
		catch
		{
			State = new T();
		}

		OnStateChange(ProviderEventType.Load);

		return State;
	}

	public virtual async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		State = state;
		LastAccessTime = DateTime.UtcNow;

		// Hash check to happen here to avoid unnecessary writes - card is coming

		try
		{
			if (Profile.AutoEncrypt)
			{
				await EncryptStateAsync(cancellationToken);
			}
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException("Could not encrypt state.", exception);
		}

		OnStateChange(ProviderEventType.Save);
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
		CipherState = string.Empty;

		OnStateChange(ProviderEventType.Clear);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Encrypts the current state into the CipherState property.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <exception cref="InvalidOperationException"></exception>
	public virtual async Task EncryptStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		try
		{
			CipherState = Cryptography.Encrypt(JsonSerializer.Serialize(State));
		}
		catch (Exception exception)
		{
			CipherState = string.Empty;
			throw new InvalidOperationException($"Could not encrypt state. Ex: {exception.Message}");
		}

		await CheckTimeoutAsync(cancellationToken);
	}

	/// <summary>
	/// Decrypts the current CipherState into the State property.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public virtual async Task DecryptStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		try
		{
			if (await CheckTimeoutAsync(cancellationToken))
			{
				State = new T();
				return;
			}

			try
			{
				string decryptedValue = Cryptography.Decrypt(CipherState);
				State = JsonSerializer.Deserialize<T>(decryptedValue);
			}
			catch
			{
				State = new T();
			}
		}
		catch (Exception exception)
		{
			State = new T();
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

	public virtual async Task TimeoutAsync()
	{
		try
		{
			if (Profile.SaveOnInstanceTimeout)
				await SaveStateAsync(State);

			await DisposeAsync();
		}
		catch
		{
			await DisposeAsync(); // force disposal no matter what
		}
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