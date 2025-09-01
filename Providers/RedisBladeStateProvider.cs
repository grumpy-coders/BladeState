using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BladeState.Providers;

public class RedisBladeStateProvider<T> : BladeStateProvider<T> where T : class, new()
{
    private readonly IDatabase _redis;
    private readonly string _key;

    public RedisBladeStateProvider(IConnectionMultiplexer redis, string key = "BladeState")
    {

        _redis = redis.GetDatabase();
        _key = $"{key}-{Profile.Id}";
    }

    public override async Task<T> LoadStateAsync()
    {
        var value = await _redis.StringGetAsync(_key);
        if (value.IsNullOrEmpty) return new T();

        return JsonSerializer.Deserialize<T>(value!);
    }

    public override async Task SaveStateAsync(T state)
    {
        var json = JsonSerializer.Serialize(state);
        await _redis.StringSetAsync(_key, json);
    }

    public override async Task ClearStateAsync()
    {
        await _redis.KeyDeleteAsync(_key);
    }
}
