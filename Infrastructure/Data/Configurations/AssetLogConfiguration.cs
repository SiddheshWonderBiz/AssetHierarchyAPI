using AssetHierarchyAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetHierarchyAPI.Infrastructure.Data.Configurations
{
    public class AssetLogConfiguration : IEntityTypeConfiguration<AssetLog>
    {
        public void Configure(EntityTypeBuilder<AssetLog> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Username)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(l => l.Role)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(l => l.Action)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(l => l.TargetName)
                   .HasMaxLength(200);

            builder.Property(l => l.TimeStamp)
                   .IsRequired();
        }
    }
}
