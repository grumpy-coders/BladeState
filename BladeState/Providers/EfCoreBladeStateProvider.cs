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
    IDbContextFactory<BladeStateDbContext> bladeStateDbContextFactory,
    BladeStateCryptography bladeStateCryptography,
    BladeStateProfile bladeStateProfile
) : BladeStateProvider<TState>(bladeStateCryptography, bladeStateProfile)
    where TState : class, new()
{
    private readonly IDbContextFactory<BladeStateDbContext> _bladeStateDbContextFactory = bladeStateDbContextFactory;

    public override async Task<TState> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return State;

            await CheckTimeoutAsync(cancellationToken);

            await using BladeStateDbContext dbContext = _bladeStateDbContextFactory.CreateDbContext();
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            BladeStateEntity entity = await dbContext.Set<BladeStateEntity>()
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

            await using BladeStateDbContext dbContext = _bladeStateDbContextFactory.CreateDbContext();

            DbSet<BladeStateEntity> set = dbContext.Set<BladeStateEntity>();
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

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await CheckTimeoutAsync(cancellationToken);
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

            await using BladeStateDbContext dbContext = _bladeStateDbContextFactory.CreateDbContext();

            DbSet<BladeStateEntity> set = dbContext.Set<BladeStateEntity>();
            try
            {
                BladeStateEntity entity = await set
                    .SingleAsync(e => e.InstanceId == Profile.InstanceId, cancellationToken)
                    .ConfigureAwait(false);

                set.Remove(entity);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // swallow if no match
            }

            CipherState = string.Empty;
            State = new TState();

            await CheckTimeoutAsync(cancellationToken);
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
