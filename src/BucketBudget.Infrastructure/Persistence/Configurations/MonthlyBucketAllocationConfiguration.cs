using BucketBudget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BucketBudget.Infrastructure.Persistence.Configurations;

public class MonthlyBucketAllocationConfiguration : IEntityTypeConfiguration<MonthlyBucketAllocation>
{
    public void Configure(EntityTypeBuilder<MonthlyBucketAllocation> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.AllocatedMilliunits).IsRequired();

        builder.HasOne(m => m.Bucket)
            .WithMany(b => b.MonthlyAllocations)
            .HasForeignKey(m => m.BucketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.BucketId, m.Year, m.Month }).IsUnique();
    }
}
