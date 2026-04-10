namespace BucketBudget.Application.Features.Accounts;

public record AccountDto(
    Guid Id,
    string Name,
    string CurrencyCode,
    long BalanceMilliunits,
    bool IsClosed,
    DateTime CreatedAt,
    DateTime UpdatedAt);
