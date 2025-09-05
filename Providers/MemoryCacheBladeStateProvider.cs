using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace BladeState.Providers;

public class MemoryCacheBladeStateProvider<T>(IMemoryCache memoryCache) : BladeStateProvider<T> where T : class, new()
{
    public override Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(new T());
        }

        if (memoryCache.TryGetValue(Profile.Id, out T state))
        {
            return Task.FromResult(state ?? new T());
        }
        else
        {
            return Task.FromResult(new T());
        }
    }

    public override Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        memoryCache.Set(Profile.Id, state, new MemoryCacheEntryOptions
        {
            SlidingExpiration = Profile.SessionTimeout
        });

        return Task.CompletedTask;
    }

    public override Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        memoryCache.Remove(Profile.Id);
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
            // swallow or log exceptions, since Dispose must not throw
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}

