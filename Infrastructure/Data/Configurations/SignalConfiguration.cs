using AssetHierarchyAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetHierarchyAPI.Infrastructure.Data.Configurations
{
    public class SignalConfiguration : IEntityTypeConfiguration<Signal>
    {
        public void Configure(EntityTypeBuilder<Signal> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(s => s.ValueType)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(s => s.Description)
                   .HasMaxLength(500);

            builder.HasOne(s => s.Asset)
                   .WithMany(a => a.Signals)
                   .HasForeignKey(s => s.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
