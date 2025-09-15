using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BladeState.Cryptography;
using BladeState.Data.EntityFrameworkCore;
using BladeState.Enums;
using BladeState.Models;
using Microsoft.EntityFrameworkCore;

namespace BladeState.Providers;

public class EfCoreBladeStateProvider<TState>(
    BladeStateDbContext bladeStateDbContext,
    BladeStateCryptography bladeStateCryptography,
    BladeStateProfile bladeStateProfile
) : BladeStateProvider<TState>(bladeStateCryptography, bladeStateProfile)
    where TState : class, new()
{
    private readonly BladeStateDbContext _dbContext = bladeStateDbContext;

    public override async Task<TState> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return State;

            await StartTimeoutTaskAsync(cancellationToken);

            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

            BladeStateEntity entity = await _dbContext.Set<BladeStateEntity>()
                .FirstAsync(e => e.InstanceId == Profile.InstanceId, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                if (Profile.AutoEncrypt)
                {
                    CipherState = entity.StateData;
                    await DecryptStateAsync(cancellationToken);
                    OnStateChange(ProviderEventType.Load);
                    return State;
                }

                State = JsonSerializer.Deserialize<TState>(entity.StateData);
                OnStateChange(ProviderEventType.Load);
                return State;
            }
            catch
            {
                State = new TState();
                OnStateChange(ProviderEventType.Load);
                return State;
            }
        }
        catch
        {
            State = new TState();
            OnStateChange(ProviderEventType.Load);
            return State;
        }
    }

    public override async Task SaveStateAsync(TState state, CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            string data;
            if (Profile.AutoEncrypt)
            {
                await EncryptStateAsync(cancellationToken);
                data = CipherState;
            }
            else
            {
                data = JsonSerializer.Serialize(state);
            }

            DbSet<BladeStateEntity> set = _dbContext.Set<BladeStateEntity>();
            try
            {
                BladeStateEntity entity = await set
                    .SingleAsync(e => e.InstanceId == Profile.InstanceId, cancellationToken)
                    .ConfigureAwait(false);

                entity.StateData = data;
                set.Update(entity);
            }
            catch
            {
                BladeStateEntity entity = new()
                {
                    InstanceId = Profile.InstanceId,
                    StateData = data
                };
                await set.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await StartTimeoutTaskAsync(cancellationToken);
            OnStateChange(ProviderEventType.Save);
        }
        catch
        {
            // swallow/log as per design
        }
    }

    public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            DbSet<BladeStateEntity> set = _dbContext.Set<BladeStateEntity>();
            try
            {
                BladeStateEntity entity = await set
                    .SingleAsync(e => e.InstanceId == Profile.InstanceId, cancellationToken)
                    .ConfigureAwait(false);

                set.Remove(entity);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // swallow if no match
            }

            CipherState = string.Empty;
            State = new TState();

            await StartTimeoutTaskAsync(cancellationToken);
            OnStateChange(ProviderEventType.Clear);
        }
        catch
        {
            CipherState = string.Empty;
            State = new TState();
            OnStateChange(ProviderEventType.Clear);
        }
    }
}
