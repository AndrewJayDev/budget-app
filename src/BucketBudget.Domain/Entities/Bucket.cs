namespace BucketBudget.Domain.Entities;

public class Bucket
{
    public Guid Id { get; set; }
    public Guid BucketGroupId { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public BucketGroup BucketGroup { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<MonthlyBucketAllocation> MonthlyAllocations { get; set; } = [];
}
