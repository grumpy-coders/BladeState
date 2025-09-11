using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
using BladeState.Enums;
using BladeState.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BladeState.Providers;

public class MemoryCacheBladeStateProvider<T>(
    IMemoryCache memoryCache,
    BladeStateCryptography bladeStateCryptography,
    BladeStateProfile bladeStateProfile
) : BladeStateProvider<T>(bladeStateCryptography, bladeStateProfile) where T : class, new()
{
    public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return State;

        await StartTimeoutTaskAsync(cancellationToken);

        try
        {
            if (memoryCache.TryGetValue(Profile.InstanceId, out string data))
            {
                if (string.IsNullOrWhiteSpace(data))
                {
                    return new T();    
                }

                if (Profile.AutoEncrypt)
                {
                    CipherState = data;
                    await DecryptStateAsync(cancellationToken);
                    OnStateChange(ProviderEventType.Load);
                    return State;
                }

                State = JsonSerializer.Deserialize<T>(data);
                OnStateChange(ProviderEventType.Load);
                return State;
            }
        }
        catch
        {
            State = new T();
            OnStateChange(ProviderEventType.Load);
            return State;
        }

        State = new T();
        OnStateChange(ProviderEventType.Load);
        return State;
    }

    public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        string data;

        try
        {
            if (Profile.AutoEncrypt)
            {
                await EncryptStateAsync(cancellationToken);
                data = CipherState;
            }
            else
            {
                data = JsonSerializer.Serialize(state);
            }

            memoryCache.Set(Profile.InstanceId, data, new MemoryCacheEntryOptions
            {
                SlidingExpiration = Profile.InstanceTimeout
            });
        }
        catch
        {
            // swallow or log serialization/encryption failures
        }

        await StartTimeoutTaskAsync(cancellationToken);

        OnStateChange(ProviderEventType.Save);
    }

    public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

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
            // swallow/log exceptions, since Dispose must not throw
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}