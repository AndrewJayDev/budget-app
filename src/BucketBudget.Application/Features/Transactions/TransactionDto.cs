namespace BucketBudget.Application.Features.Transactions;

public record TransactionDto(
    Guid Id,
    Guid AccountId,
    Guid? BucketId,
    string Payee,
    long AmountMilliunits,
    DateOnly Date,
    string? Memo,
    bool IsCleared,
    DateTime CreatedAt,
    DateTime UpdatedAt);
