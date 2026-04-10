namespace BucketBudget.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string CurrencyCode { get; set; }
    public long BalanceMilliunits { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
}
