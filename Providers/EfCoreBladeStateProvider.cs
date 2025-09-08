using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
using BladeState.Models;
using Microsoft.EntityFrameworkCore;

namespace BladeState.Providers;

public class EfCoreBladeStateProvider<T>(
    DbContext dbContext,
    BladeStateCryptography bladeStateCryptography,
    BladeStateProfile bladeStateProfile
) : BladeStateProvider<T>(bladeStateCryptography, bladeStateProfile) where T : class, new()
{
    private readonly DbContext _dbContext = dbContext;

    public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return State;

        await StartTimeoutTaskAsync(cancellationToken);

        BladeStateEntity entity;

        try
        {
            entity = await _dbContext.Set<BladeStateEntity>()
                .FirstAsync(e => e.InstanceId == Profile.InstanceId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            State = new T();
            return State;
        }

        if (Profile.AutoEncrypt)
        {
            CipherState = entity.StateData;
            DecryptState(cancellationToken);
            return State;
        }

        State = JsonSerializer.Deserialize<T>(entity.StateData);
        return State;
    }

    public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        string data;

        if (Profile.AutoEncrypt)
        {
            EncryptState(cancellationToken);
            data = CipherState;
        }
        else
        {
            data = JsonSerializer.Serialize(state);
        }

        BladeStateEntity entity;
        DbSet<BladeStateEntity> set = _dbContext.Set<BladeStateEntity>();

        try
        {
            entity = await set.SingleAsync(e => e.InstanceId == Profile.InstanceId, cancellationToken).ConfigureAwait(false);
            entity.StateData = data;

            set.Update(entity);
        }
        catch
        {
            entity = new BladeStateEntity { InstanceId = Profile.InstanceId, StateData = data };

            await set.AddAsync(entity, cancellationToken)
                .ConfigureAwait(false);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        DbSet<BladeStateEntity> set = _dbContext.Set<BladeStateEntity>();
        BladeStateEntity entity;

        try
        {
            entity = await set.SingleAsync(e => e.InstanceId == Profile.InstanceId, cancellationToken)
                .ConfigureAwait(false);
            set.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            throw new KeyNotFoundException($"InstanceId: '{Profile.InstanceId}' was not found in the DbSet");
        }

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
