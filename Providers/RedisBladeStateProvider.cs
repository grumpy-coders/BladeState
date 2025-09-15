using BladeState.Cryptography;
using BladeState.Enums;
using BladeState.Models;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BladeState.Providers;

public class RedisBladeStateProvider<T>(
	IConnectionMultiplexer redis,
	BladeStateCryptography bladeStateCryptography,
	BladeStateProfile bladeStateProfile
) : BladeStateProvider<T>(bladeStateCryptography, bladeStateProfile) where T : class, new()
{
	private readonly IDatabase _redis = redis.GetDatabase();

	private string GetKey() => $"BladeState:{Profile.InstanceName}-{Profile.InstanceId}";

	public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return State;
		}

		await StartTimeoutTaskAsync(cancellationToken);

		RedisValue redisValue = await _redis.StringGetAsync(GetKey()).ConfigureAwait(false);

		if (redisValue.IsNullOrEmpty)
		{
			State = new T();
			OnStateChange(ProviderEventType.Load);
			return State;
		}

		if (Profile.AutoEncrypt)
		{
			CipherState = redisValue;
			await DecryptStateAsync(cancellationToken);
			OnStateChange(ProviderEventType.Load);
			return State;
		}

		State = JsonSerializer.Deserialize<T>(redisValue);
		OnStateChange(ProviderEventType.Load);

		return State;
	}

	public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		if (Profile.AutoEncrypt)
		{
			await EncryptStateAsync(cancellationToken);
			await _redis.StringSetAsync(GetKey(), CipherState).ConfigureAwait(false);
			return;
		}

		await _redis.StringSetAsync(GetKey(), JsonSerializer.Serialize(state)).ConfigureAwait(false);

		await StartTimeoutTaskAsync(cancellationToken);

		OnStateChange(ProviderEventType.Save);
	}

	public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		await _redis.KeyDeleteAsync(GetKey()).ConfigureAwait(false);

		CipherState = string.Empty;
		State = new T();

		await StartTimeoutTaskAsync(cancellationToken);

		OnStateChange(ProviderEventType.Clear);
	}

	/// <summary>
	/// Async disposal hook: cleanup persisted state before disposal.
	/// </summary>
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await ClearStateAsync(CancellationToken.None).ConfigureAwait(false);
		}
		catch
		{
			// swallow or log exceptions, since Dispose must not throw
		}

		await base.DisposeAsyncCore().ConfigureAwait(false);
	}
}
