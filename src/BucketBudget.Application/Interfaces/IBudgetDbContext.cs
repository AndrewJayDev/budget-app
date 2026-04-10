using BucketBudget.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Interfaces;

public interface IBudgetDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Bucket> Buckets { get; }
    DbSet<BucketGroup> BucketGroups { get; }
    DbSet<MonthlyBucketAllocation> MonthlyBucketAllocations { get; }
    DbSet<RecurringTransaction> RecurringTransactions { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
