namespace BucketBudget.Domain.Entities;

public class BucketGroup
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Bucket> Buckets { get; set; } = [];
}
