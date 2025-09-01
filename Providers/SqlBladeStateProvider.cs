using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BladeState.Providers;

public class SqlBladeStateProvider<T>(DbContext dbContext) : BladeStateProvider<T> where T : class, new()
{
    private readonly DbContext _dbContext = dbContext;
    public string StateId { get; set; } = Guid.NewGuid().ToString();

    public override async Task<T> LoadStateAsync()
    {
        // Assumes table maps to T
        return await _dbContext.Set<T>().FirstOrDefaultAsync();
    }

    public async override Task SaveStateAsync(T state)
    {
        var set = _dbContext.Set<T>();
        if (_dbContext.Entry(state).IsKeySet)
        {
            set.Update(state);
        }
        else
        {
            set.Add(state);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async override Task ClearStateAsync()
    {
        var set = _dbContext.Set<T>();
        set.RemoveRange(set);
        await _dbContext.SaveChangesAsync();
    }
}
