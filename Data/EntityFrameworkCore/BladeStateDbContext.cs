using BladeState.Models;
using Microsoft.EntityFrameworkCore;

namespace BladeState.Data.EntityFrameworkCore;

public class BladeStateDbContext : DbContext
{

    public BladeStateDbContext(DbContextOptions<BladeStateDbContext> options)
      : base(options)
    {
    }
    
    public DbSet<BladeStateEntity> BladeState { get; set; } = default;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BladeStateEntity>()
            .HasKey(e => e.InstanceId);
    }
}
