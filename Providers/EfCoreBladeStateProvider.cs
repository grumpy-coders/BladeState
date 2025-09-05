using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BladeState.Providers;

public class EfCoreBladeStateProvider<T>(DbContext dbContext) : BladeStateProvider<T> where T : class, new()
{
    private readonly DbContext _dbContext = dbContext;

    public override async Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().FirstOrDefaultAsync(cancellationToken) ?? new T();
    }

    public override async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        DbSet<T> set = _dbContext.Set<T>();
        if (_dbContext.Entry(state).IsKeySet)
        {
            set.Update(state);
        }
        else
        {
            await set.AddAsync(state, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public override async Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        DbSet<T> set = _dbContext.Set<T>();
        set.RemoveRange(set);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // fire and forget
            try
            {
                ClearStateAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch
            {
                // optional: swallow/log exceptions, since Dispose should not throw
            }
        }

        base.Dispose(disposing);
    }
}
