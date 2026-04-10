using BucketBudget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BucketBudget.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Payee).HasMaxLength(200).IsRequired();
        builder.Property(t => t.AmountMilliunits).IsRequired();
        builder.Property(t => t.Memo).HasMaxLength(500);

        builder.HasOne(t => t.Bucket)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.BucketId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.Date);
        builder.HasIndex(t => t.AccountId);
    }
}
