using AssetHierarchyAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Security.Cryptography;
using System.Text;

namespace AssetHierarchyAPI.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.UserEmail)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(u => u.Password)
                   .HasMaxLength(500);

            builder.Property(u => u.Role)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(u => u.Username).IsUnique();

            // Seed admin user
            builder.HasData(new User
            {
                Id = 1,
                Username = "admin",
                UserEmail = "admin123@gmail.com",
                Password = Convert.ToBase64String(
                    SHA256.HashData(Encoding.UTF8.GetBytes("Admin@123"))
                ),
                Role = "Admin"
            });
        }
    }
}
