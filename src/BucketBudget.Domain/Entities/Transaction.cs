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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public Bucket? Bucket { get; set; }
}
