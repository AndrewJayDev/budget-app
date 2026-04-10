namespace BucketBudget.Domain.Entities;

public class ExchangeRate
{
    public Guid Id { get; set; }
    public required string FromCurrencyCode { get; set; }
    public required string ToCurrencyCode { get; set; }
    public decimal Rate { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
