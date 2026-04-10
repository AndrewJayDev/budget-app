using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using BucketBudget.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BucketBudget.Infrastructure.RecurringTransactions;

public class RecurringTransactionPostingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringTransactionPostingService> _logger;

    public RecurringTransactionPostingService(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurringTransactionPostingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PostDueTransactionsAsync(stoppingToken);

            // Run again at the start of the next day (UTC)
            var nextRun = DateTime.UtcNow.Date.AddDays(1);
            var delay = nextRun - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);
        }
    }

    public async Task PostDueTransactionsAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IBudgetDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var due = await db.RecurringTransactions
            .Where(r => r.AutoPost
                && r.NextOccurrence.HasValue
                && r.NextOccurrence.Value <= today
                && (!r.EndDate.HasValue || r.EndDate.Value >= today))
            .ToListAsync(ct);

        if (due.Count == 0)
            return;

        _logger.LogInformation("Auto-posting {Count} due recurring transactions", due.Count);

        foreach (var recurring in due)
        {
            // Post all occurrences up to and including today
            while (recurring.NextOccurrence.HasValue
                   && recurring.NextOccurrence.Value <= today
                   && (!recurring.EndDate.HasValue || recurring.NextOccurrence.Value <= recurring.EndDate.Value))
            {
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = recurring.AccountId,
                    BucketId = recurring.BucketId,
                    Payee = recurring.Payee,
                    AmountMilliunits = recurring.AmountMilliunits,
                    Date = recurring.NextOccurrence.Value,
                    Memo = recurring.Memo,
                    IsCleared = false
                };
                db.Transactions.Add(transaction);

                _logger.LogDebug(
                    "Posted recurring transaction {RecurringId} for {Date}: {Payee}",
                    recurring.Id, recurring.NextOccurrence.Value, recurring.Payee);

                recurring.NextOccurrence = ComputeNextOccurrence(recurring.NextOccurrence.Value, recurring.Frequency);
            }

            // If past EndDate, clear NextOccurrence
            if (recurring.EndDate.HasValue && recurring.NextOccurrence.HasValue
                && recurring.NextOccurrence.Value > recurring.EndDate.Value)
            {
                recurring.NextOccurrence = null;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public static DateOnly ComputeNextOccurrence(DateOnly current, RecurrenceFrequency frequency) =>
        frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(1),
            RecurrenceFrequency.Weekly => current.AddDays(7),
            RecurrenceFrequency.Biweekly => current.AddDays(14),
            RecurrenceFrequency.Monthly => current.AddMonths(1),
            RecurrenceFrequency.Yearly => current.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };
}
