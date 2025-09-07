using BladeState.Cryptography;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BladeState.Providers;

public class RedisBladeStateProvider<T>(
	IConnectionMultiplexer redis,
	BladeStateCryptography bladeStateCryptography
) : BladeStateProvider<T>(bladeStateCryptography) where T : class, new()
{
	private readonly IDatabase _redis = redis.GetDatabase();

	private string GetKey() => $"{Profile.InstanceName}-{Profile.InstanceId}";

	public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return State;
		}

		RedisValue redisValue = await _redis.StringGetAsync(GetKey()).ConfigureAwait(false);

		if (redisValue.IsNullOrEmpty)
		{
			State = new T();
			return State;
		}

		if (Profile.AutoEncrypt)
		{
			CipherState = redisValue;
			DecryptState();
			return State;
		}

		State = JsonSerializer.Deserialize<T>(redisValue);
		return State;
	}

	public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		if (Profile.AutoEncrypt)
		{
			EncryptState();
			await _redis.StringSetAsync(GetKey(), CipherState).ConfigureAwait(false);
			return;
		}

		await _redis.StringSetAsync(GetKey(), JsonSerializer.Serialize(state)).ConfigureAwait(false);
	}

	public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		await _redis.KeyDeleteAsync(GetKey()).ConfigureAwait(false);

		CipherState = string.Empty;
		State = new T();
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
