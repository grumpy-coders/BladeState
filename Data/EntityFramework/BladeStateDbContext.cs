using System.ComponentModel.DataAnnotations.Schema;
using BladeState.Models;
using Microsoft.EntityFrameworkCore;

namespace BladeState.Data;

public class BladeStateDbContext : DbContext
{
    public DbSet<BladeStateEntity> BladeState { get; set; } = default;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BladeStateEntity>()     
            .HasKey(e => e.InstanceId);
    }
}
