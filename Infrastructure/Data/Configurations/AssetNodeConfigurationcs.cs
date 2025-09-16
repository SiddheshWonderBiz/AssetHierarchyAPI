using AssetHierarchyAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetHierarchyAPI.Infrastructure.Data.Configurations
{
    public class AssetNodeConfiguration : IEntityTypeConfiguration<AssetNode>
    {
        public void Configure(EntityTypeBuilder<AssetNode> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Name).IsRequired().HasMaxLength(100);

            builder.HasOne(a => a.Parent)
                   .WithMany(a => a.Children)
                   .HasForeignKey(a => a.ParentId);

            builder.HasMany(a => a.Signals)
                   .WithOne(s => s.Asset)
                   .HasForeignKey(s => s.AssetId);
        }
    }
}
