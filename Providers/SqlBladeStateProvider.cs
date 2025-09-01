using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BladeState.Providers;

public class SqlBladeStateProvider<T> : IBladeStateProvider<T> where T : class, new()
{
    private readonly DbContext _dbContext;

    public SqlBladeStateProvider(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> LoadStateAsync()
    {
        // Assumes table maps to T
        return await _dbContext.Set<T>().FirstOrDefaultAsync();
    }

    public async Task SaveStateAsync(T state)
    {
        var set = _dbContext.Set<T>();
        if (!_dbContext.Entry(state).IsKeySet)
            set.Add(state);
        else
            set.Update(state);

        await _dbContext.SaveChangesAsync();
    }

    public async Task ClearStateAsync()
    {
        var set = _dbContext.Set<T>();
        set.RemoveRange(set);
        await _dbContext.SaveChangesAsync();
    }
}
