using AssetHierarchyAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AssetHierarchyAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {        }
        public DbSet<AssetNode> AssetNodes { get; set; }
        public DbSet<Signals> Signals { get; set; }

        public DbSet<User> Users { get; set; }
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
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
         
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                Password = Convert.ToBase64String(
        SHA256.HashData(Encoding.UTF8.GetBytes("Admin@123"))
    ),
                Role = "Admin"
            });
        }
    }
}
