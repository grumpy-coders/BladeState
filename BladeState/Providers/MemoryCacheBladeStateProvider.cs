using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GrumpyCoders.BladeState.Cryptography;
using GrumpyCoders.BladeState.Enums;
using GrumpyCoders.BladeState.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GrumpyCoders.BladeState.Providers;

public class MemoryCacheBladeStateProvider<T>(IMemoryCache memoryCache, BladeStateCryptography bladeStateCryptography, BladeStateProfile bladeStateProfile) : BladeStateProvider<T>(bladeStateCryptography, bladeStateProfile) where T : class, new()
{
	public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return State;
		}

		await CheckTimeoutAsync(cancellationToken);

		try
		{
			if (memoryCache.TryGetValue(Profile.InstanceId, out string data) && !string.IsNullOrWhiteSpace(data))
			{
				if (Profile.AutoEncrypt)
				{
					data = Decrypt(data);

				}
				State = JsonSerializer.Deserialize<T>(data) ?? new T();
			}
			else
			{
				State = new T();
			}
		}
		catch
		{
			State = new T();
		}

		OnStateChange(ProviderEventType.Load);
		return State;
	}

	public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		await CheckTimeoutAsync(cancellationToken);
		LastAccessTime = DateTime.Now;
		State = state;

		try
		{
			string data = Profile.AutoEncrypt ? EncryptState() : JsonSerializer.Serialize(State);
			memoryCache.Set(Profile.InstanceId, data, new MemoryCacheEntryOptions { SlidingExpiration = Profile.InstanceTimeout });
		}
		catch
		{
			// swallow or log serialization/encryption failures
		}

		OnStateChange(ProviderEventType.Save);
	}

	public override Task ClearStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return Task.CompletedTask;

		try
		{
			memoryCache.Remove(Profile.InstanceId);
		}
		catch
		{
			// swallow/log
		}

		State = new T();
		OnStateChange(ProviderEventType.Clear);
		return Task.CompletedTask;

	}

	/// <summary>
	/// Async disposal hook: cleanup persisted state before disposal.
	/// </summary>
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			if (Profile.AutoClearOnDispose)
			{
				await ClearStateAsync(CancellationToken.None).ConfigureAwait(false);
			}
		}
		catch
		{
			// swallow/log exceptions, since Dispose must not throw
		}

		await base.DisposeAsyncCore().ConfigureAwait(false);
	}
}
