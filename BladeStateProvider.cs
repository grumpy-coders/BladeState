using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
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

	private DateTime _lastLoadTime;
	private CancellationTokenSource _timeoutCancellationTokenSource = new CancellationTokenSource();
	private Task _timeoutTask;
	private readonly SemaphoreSlim _timeoutLock = new SemaphoreSlim(1, 1);
	private bool _disposed;

	public virtual async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return State;
		try
		{
			if (Profile.AutoEncrypt)
				DecryptState(cancellationToken);
		}
		catch
		{
			State = new T();
		}
		_lastLoadTime = DateTime.UtcNow;

		await StartTimeoutTaskAsync(cancellationToken);

		return State;
	}

	public virtual Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled(cancellationToken);

		State = state;

		try
		{
			if (Profile.AutoEncrypt)
				EncryptState(cancellationToken);
		}
		catch
		{

		}

		return Task.CompletedTask;
	}


	public virtual async Task ClearStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		State = new T();
		CipherState = string.Empty;

		await Task.CompletedTask;
	}

	public virtual void EncryptState(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		try
		{
			CipherState = Cryptography.Encrypt(JsonSerializer.Serialize(State));
		}
		catch
		{
			CipherState = string.Empty;
		}
	}

	public virtual void DecryptState(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		try
		{
			State = JsonSerializer.Deserialize<T>(Cryptography.Decrypt(CipherState)) ?? new T();
		}
		catch
		{
			State = new T();
		}
	}

	protected async Task StartTimeoutTaskAsync(CancellationToken cancellationToken = default)
	{
		await _timeoutLock.WaitAsync(cancellationToken);
		try
		{
			// Cancel and dispose previous timeout CancellationTokenSource
			_timeoutCancellationTokenSource.Cancel();
			_timeoutCancellationTokenSource.Dispose();
			_timeoutCancellationTokenSource = new CancellationTokenSource();

			// Combine internal CancellationTokenSource (for resets) with external token
			using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_timeoutCancellationTokenSource.Token, cancellationToken);
            CancellationToken token = linkedCancellationTokenSource.Token;

			_timeoutTask = Task.Run(async () =>
			{
				try
				{
					TimeSpan delay = Profile.InstanceTimeout;
					TimeSpan elapsed = DateTime.UtcNow - _lastLoadTime;
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

			await ClearStateAsync();
			await DisposeAsync();
		}
		catch
		{
			await DisposeAsync(); //force disposal, even if save or clear fails (same concept of leaving toys on the floor and mom vacuums them up :P)
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

			if (_timeoutTask != null)
				await _timeoutTask;

			await DisposeAsyncCore();
			GC.SuppressFinalize(this);
			_disposed = true;
		}
	}

	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}