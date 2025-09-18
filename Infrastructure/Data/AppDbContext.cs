using AssetHierarchyAPI.Domain.Models;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyAPI.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AssetNode> AssetNodes { get; set; }
        public DbSet<Signal> Signals { get; set; }
        public DbSet<SignalValue> SignalValues { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AssetLog> AssetLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Automatically apply all IEntityTypeConfiguration classes
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
