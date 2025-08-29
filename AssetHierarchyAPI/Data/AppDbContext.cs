using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {        }
        public DbSet<AssetNode> AssetNodes { get; set; }
        public DbSet<Signals> Signals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AssetNode>()
                .HasOne(n => n.Parent)                 // Each node has one Parent
                .WithMany(n => n.Children)             // A parent can have many Children
                .HasForeignKey(n => n.ParentId)        // FK column
                .OnDelete(DeleteBehavior.ClientCascade);     // If parent deleted, children also deleted

           modelBuilder.Entity<AssetNode>()
                .HasMany(a => a.Signals)
                .WithOne(s => s.Asset)
                .HasForeignKey(s => s.AssetId)
                .OnDelete(DeleteBehavior.Cascade);


        }
    }
}
