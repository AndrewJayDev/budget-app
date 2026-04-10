namespace BucketBudget.Client;

public record AccountDto(
    Guid Id,
    string Name,
    string CurrencyCode,
    long BalanceMilliunits,
    bool IsClosed,
    DateTime CreatedAt,
    DateTime UpdatedAt);

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

public record RecurringTransactionDto(
    Guid Id,
    Guid AccountId,
    Guid? BucketId,
    string Payee,
    long AmountMilliunits,
    string? Memo,
    string Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? NextOccurrence,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record BucketSummaryDto(Guid Id, string Name, int SortOrder, long AllocatedMilliunits, long ActivityMilliunits, long AvailableMilliunits);
public record BucketGroupSummaryDto(Guid Id, string Name, int SortOrder, List<BucketSummaryDto> Buckets);
public record MonthBudgetDto(int Year, int Month, List<BucketGroupSummaryDto> BucketGroups);

public record ExchangeRateDto(
    Guid Id,
    string FromCurrencyCode,
    string ToCurrencyCode,
    decimal Rate,
    string RateType,
    DateOnly EffectiveDate,
    DateTime CreatedAt);

public record RunRecurringResult(int Created, List<Guid> TransactionIds);

public record CreateTransactionRequest(
    Guid AccountId,
    Guid? BucketId,
    string Payee,
    long AmountMilliunits,
    DateOnly Date,
    string? Memo,
    bool IsCleared);

public record UpsertAllocationRequest(long AllocatedMilliunits);

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Email);
