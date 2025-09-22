using BladeState.Models;
using Microsoft.EntityFrameworkCore;

namespace BladeState.Data.EntityFrameworkCore
{
    public class BladeStateDbContext : DbContext
    {
        public BladeStateDbContext(DbContextOptions<BladeStateDbContext> options)
            : base(options)
        {
        }

        public DbSet<BladeStateEntity> BladeState { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure BladeStateEntity schema
            modelBuilder.Entity<BladeStateEntity>(entity =>
            {
                entity.HasKey(e => e.InstanceId);
                entity.Property(e => e.StateData).IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
