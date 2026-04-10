using BucketBudget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BucketBudget.Infrastructure.Persistence.Configurations;

public class RecurringTransactionConfiguration : IEntityTypeConfiguration<RecurringTransaction>
{
    public void Configure(EntityTypeBuilder<RecurringTransaction> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Payee).HasMaxLength(200).IsRequired();
        builder.Property(r => r.AmountMilliunits).IsRequired();
        builder.Property(r => r.Memo).HasMaxLength(500);
        builder.Property(r => r.Frequency)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(r => r.Account)
            .WithMany()
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Bucket)
            .WithMany()
            .HasForeignKey(r => r.BucketId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
