using BucketBudget.Domain.Enums;

namespace BucketBudget.Application.Features.RecurringTransactions;

public record RecurringTransactionDto(
    Guid Id,
    Guid AccountId,
    Guid? BucketId,
    string Payee,
    long AmountMilliunits,
    string? Memo,
    RecurrenceFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? NextOccurrence,
    DateTime CreatedAt,
    DateTime UpdatedAt);
