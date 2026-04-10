namespace BucketBudget.Domain.Entities;

public class MonthlyBucketAllocation
{
    public Guid Id { get; set; }
    public Guid BucketId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public long AllocatedMilliunits { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Bucket Bucket { get; set; } = null!;
}
