using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {        }
        public DbSet<AssetNode> AssetNodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssetNode>()
                .HasOne(n => n.Parent)                 // Each node has one Parent
                .WithMany(n => n.Children)             // A parent can have many Children
                .HasForeignKey(n => n.ParentId)        // FK column
                .OnDelete(DeleteBehavior.ClientCascade);     // If parent deleted, children also deleted

           //Add unique constraint for Name under same parent
            modelBuilder.Entity<AssetNode>()
                .HasIndex(n => new { n.Name, n.ParentId })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
