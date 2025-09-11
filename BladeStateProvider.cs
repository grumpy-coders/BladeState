using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
using BladeState.Enums;
using BladeState.Events;
using BladeState.Models;

namespace BladeState;

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
	private CancellationTokenSource _timeoutCancellationTokenSource = new();
	private Task _timeoutTask;
	private readonly SemaphoreSlim _timeoutLock = new(1, 1);
	private bool _disposed;

	public event EventHandler<BladeStateProviderEventArgs<T>> StateChanged = delegate { };

	/// <summary>
	/// Fires when a change occurs to the State. Such as after the State is loaded, saved, cleared. Can be used to update components, screens, force actions to occur, etc.
	/// </summary>
	/// <param name="eventType">The ProviderEventType to denote the type of change to the state</param>
	/// <returns></returns>
	protected void OnStateChange(ProviderEventType eventType = ProviderEventType.None) =>
		StateChanged(this, new BladeStateProviderEventArgs<T>(Profile.InstanceId, State, eventType));


	public virtual async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return State;

		try
		{
			if (Profile.AutoEncrypt)
				await DecryptStateAsync(cancellationToken);
		}
		catch
		{
			State = new T();
		}

		await StartTimeoutTaskAsync(cancellationToken);

		OnStateChange(ProviderEventType.Load);

		return State;
	}

	public virtual async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		State = state;

		try
		{
			if (Profile.AutoEncrypt)
				await EncryptStateAsync(cancellationToken);
		}
		catch
		{
			// swallow encryption errors
		}

		await StartTimeoutTaskAsync(cancellationToken);

		OnStateChange(ProviderEventType.Save);
	}

	public virtual async Task ClearStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		State = new T();
		CipherState = string.Empty;

		await StartTimeoutTaskAsync(cancellationToken);

		OnStateChange(ProviderEventType.Clear);
	}

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

		await StartTimeoutTaskAsync(cancellationToken);
	}

	public virtual async Task DecryptStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		try
		{
			string decryptedValue = Cryptography.Decrypt(CipherState);

			try
			{
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

		await StartTimeoutTaskAsync(cancellationToken);
	}

	protected async Task StartTimeoutTaskAsync(CancellationToken cancellationToken = default)
	{
		LastAccessTime = DateTime.UtcNow;

		await _timeoutLock.WaitAsync(cancellationToken);
		try
		{
			_timeoutCancellationTokenSource.Cancel();
			_timeoutCancellationTokenSource.Dispose();
			_timeoutCancellationTokenSource = new CancellationTokenSource();

			using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_timeoutCancellationTokenSource.Token, cancellationToken);
			CancellationToken token = linkedCancellationTokenSource.Token;

			_timeoutTask = Task.Run(async () =>
			{
				try
				{
					TimeSpan delay = Profile.InstanceTimeout;
					TimeSpan elapsed = DateTime.UtcNow - LastAccessTime;
					TimeSpan remaining = delay - elapsed;

					if (remaining > TimeSpan.Zero)
						await Task.Delay(remaining, token);

					if (!token.IsCancellationRequested)
						await TimeoutAsync();
				}
				catch (TaskCanceledException)
				{
					// Ignore cancellation
				}
			}, token);
		}
		finally
		{
			_timeoutLock.Release();
		}
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
			await _timeoutLock.WaitAsync();
			try
			{
				_timeoutCancellationTokenSource.Cancel();
				_timeoutCancellationTokenSource.Dispose();
			}
			finally
			{
				_timeoutLock.Release();
			}

			await _timeoutTask;

			await DisposeAsyncCore();
			GC.SuppressFinalize(this);
			_disposed = true;
		}
	}

	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}