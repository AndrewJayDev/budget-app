namespace BucketBudget.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? BucketId { get; set; }
    public required string Payee { get; set; }
    public long AmountMilliunits { get; set; }
    public DateOnly Date { get; set; }
    public string? Memo { get; set; }
    public bool IsCleared { get; set; }

    // Cross-currency transfer: links the two legs of a transfer between different-currency accounts
    public Guid? TransferPairId { get; set; }
    // The exchange rate used when this was a cross-currency transfer
    public Guid? ExchangeRateId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public Bucket? Bucket { get; set; }
    public ExchangeRate? ExchangeRate { get; set; }
}
