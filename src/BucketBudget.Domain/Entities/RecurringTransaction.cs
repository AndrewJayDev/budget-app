using BucketBudget.Domain.Enums;

namespace BucketBudget.Domain.Entities;

public class RecurringTransaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? BucketId { get; set; }
    public required string Payee { get; set; }
    public long AmountMilliunits { get; set; }
    public string? Memo { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? NextOccurrence { get; set; }
    public bool AutoPost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public Bucket? Bucket { get; set; }
}
