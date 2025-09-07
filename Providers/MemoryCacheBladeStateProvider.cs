using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
using BladeState.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BladeState.Providers;

public class MemoryCacheBladeStateProvider<T>(
    IMemoryCache memoryCache,
    BladeStateCryptography bladeStateCryptography,
    BladeStateProfile bladeStateProfile
) : BladeStateProvider<T>(bladeStateCryptography, bladeStateProfile) where T : class, new()
{
    public override Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromResult(State);

        try
        {
            if (memoryCache.TryGetValue(Profile.InstanceId, out string data))
            {
                if (Profile.AutoEncrypt)
                {
                    CipherState = data;
                    DecryptState();
                    return Task.FromResult(State);
                }

                State = JsonSerializer.Deserialize<T>(data);
                return Task.FromResult(State);
            }
        }
        catch
        {
            State = new T();
            return Task.FromResult(State);
        }

        State = new T();
        return Task.FromResult(State);
    }

    public override Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.CompletedTask;

        string data;

        try
        {
            if (Profile.AutoEncrypt)
            {
                EncryptState();
                data = CipherState;
            }
            else
            {
                data = JsonSerializer.Serialize(state);
            }

            memoryCache.Set(Profile.InstanceId, data, new MemoryCacheEntryOptions
            {
                SlidingExpiration = Profile.Timeout
            });
        }
        catch
        {
            // swallow or log serialization/encryption failures
        }

        return Task.CompletedTask;
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

        CipherState = string.Empty;
        State = new T();

        return Task.CompletedTask;
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
            // swallow/log exceptions, since Dispose must not throw
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}