using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using BucketBudget.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.RecurringTransactions.Commands;

public record RunRecurringTransactionsCommand(DateOnly? AsOf) : IRequest<RunRecurringTransactionsResult>;

public record RunRecurringTransactionsResult(int Created, List<Guid> TransactionIds);

public class RunRecurringTransactionsCommandHandler : IRequestHandler<RunRecurringTransactionsCommand, RunRecurringTransactionsResult>
{
    private readonly IBudgetDbContext _context;
    public RunRecurringTransactionsCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task<RunRecurringTransactionsResult> Handle(RunRecurringTransactionsCommand request, CancellationToken ct)
    {
        var asOf = request.AsOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var due = await _context.RecurringTransactions
            .Where(r => r.NextOccurrence.HasValue && r.NextOccurrence.Value <= asOf)
            .Where(r => r.EndDate == null || r.EndDate.Value >= asOf)
            .ToListAsync(ct);

        var createdIds = new List<Guid>();

        foreach (var recurring in due)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = recurring.AccountId,
                BucketId = recurring.BucketId,
                Payee = recurring.Payee,
                AmountMilliunits = recurring.AmountMilliunits,
                Date = recurring.NextOccurrence!.Value,
                Memo = recurring.Memo,
                IsCleared = false
            };

            _context.Transactions.Add(transaction);

            // Advance next occurrence
            recurring.NextOccurrence = AdvanceDate(recurring.NextOccurrence.Value, recurring.Frequency);
            recurring.UpdatedAt = DateTime.UtcNow;

            createdIds.Add(transaction.Id);
        }

        await _context.SaveChangesAsync(ct);
        return new RunRecurringTransactionsResult(createdIds.Count, createdIds);
    }

    private static DateOnly AdvanceDate(DateOnly date, RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Daily => date.AddDays(1),
        RecurrenceFrequency.Weekly => date.AddDays(7),
        RecurrenceFrequency.Biweekly => date.AddDays(14),
        RecurrenceFrequency.Monthly => date.AddMonths(1),
        RecurrenceFrequency.Yearly => date.AddYears(1),
        _ => date.AddMonths(1)
    };
}
