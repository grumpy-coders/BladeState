using StackExchange.Redis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BladeState.Providers;

public class RedisBladeStateProvider<T>(IConnectionMultiplexer redis, string keyPrefix = "BladeState") : BladeStateProvider<T> where T : class, new()
{
	private readonly IDatabase _redis = redis.GetDatabase();
	private readonly string _keyPrefix = keyPrefix;

	private string GetKey() => $"{_keyPrefix}-{Profile.Id}";

	public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
	{
		RedisValue value = await _redis.StringGetAsync(GetKey()).ConfigureAwait(false);
		if (cancellationToken.IsCancellationRequested)
			return new T();

		return value.IsNullOrEmpty
			 ? new T()
			 : JsonSerializer.Deserialize<T>(value!) ?? new T();
	}

	public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		string json = JsonSerializer.Serialize(state);
		await _redis.StringSetAsync(GetKey(), json).ConfigureAwait(false);
	}

	public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		await _redis.KeyDeleteAsync(GetKey()).ConfigureAwait(false);
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
