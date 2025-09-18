using AssetHierarchyAPI.Domain.Models;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetHierarchyAPI.Infrastructure.Data.Configurations
{
    public class SignalValueConfiguration : IEntityTypeConfiguration<SignalValue>
    {
        public void Configure(EntityTypeBuilder<SignalValue> builder)
        {
            builder.HasKey(sv => sv.Id);

            builder.Property(sv => sv.Value)
                   .IsRequired();

            builder.HasOne(sv => sv.Signal)
                   .WithMany(s => s.Values)
                   .HasForeignKey(sv => sv.SignalId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
