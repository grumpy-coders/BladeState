using StackExchange.Redis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BladeState.Providers;

public class RedisBladeStateProvider<T> : BladeStateProvider<T> where T : class, new()
{
    private readonly IDatabase _redis;
    private readonly string _keyPrefix;

    public RedisBladeStateProvider(IConnectionMultiplexer redis, string keyPrefix = "BladeState")
    {
        _redis = redis.GetDatabase();
        _keyPrefix = keyPrefix;
    }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // cleanup persisted state (optional) before disposal
                ClearStateAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // swallow/log exceptions, since Dispose must not throw
            }
        }

        base.Dispose(disposing);
    }
}
