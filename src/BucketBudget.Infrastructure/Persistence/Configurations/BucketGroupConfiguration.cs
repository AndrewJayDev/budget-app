using BucketBudget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BucketBudget.Infrastructure.Persistence.Configurations;

public class BucketGroupConfiguration : IEntityTypeConfiguration<BucketGroup>
{
    public void Configure(EntityTypeBuilder<BucketGroup> builder)
    {
        builder.HasKey(bg => bg.Id);
        builder.Property(bg => bg.Name).HasMaxLength(200).IsRequired();

        builder.HasMany(bg => bg.Buckets)
            .WithOne(b => b.BucketGroup)
            .HasForeignKey(b => b.BucketGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
