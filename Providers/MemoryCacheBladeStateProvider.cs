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
            return State ?? new T();

        await StartTimeoutTaskAsync(cancellationToken);

        try
        {
            if (memoryCache.TryGetValue(Profile.InstanceId, out string data) && !string.IsNullOrWhiteSpace(data))
            {
                if (Profile.AutoEncrypt)
                {
                    CipherState = data;
                    await DecryptStateAsync(cancellationToken);
                }
                else
                {
                    State = JsonSerializer.Deserialize<T>(data) ?? new T();
                }
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
            return;

        State = state ?? new T();

        try
        {
            string data;
            if (Profile.AutoEncrypt)
            {
                await EncryptStateAsync(cancellationToken);
                data = CipherState;
            }
            else
            {
                data = JsonSerializer.Serialize(State);
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
