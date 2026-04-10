using BucketBudget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BucketBudget.Infrastructure.Persistence.Configurations;

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FromCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(e => e.ToCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(e => e.Rate).HasPrecision(18, 8).IsRequired();

        builder.HasIndex(e => new { e.FromCurrencyCode, e.ToCurrencyCode, e.EffectiveDate }).IsUnique();
    }
}
