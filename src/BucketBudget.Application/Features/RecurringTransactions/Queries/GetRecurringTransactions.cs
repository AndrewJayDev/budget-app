using BucketBudget.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.RecurringTransactions.Queries;

public record GetRecurringTransactionsQuery(Guid? AccountId) : IRequest<List<RecurringTransactionDto>>;

public class GetRecurringTransactionsQueryHandler : IRequestHandler<GetRecurringTransactionsQuery, List<RecurringTransactionDto>>
{
    private readonly IBudgetDbContext _context;
    public GetRecurringTransactionsQueryHandler(IBudgetDbContext context) => _context = context;

    public async Task<List<RecurringTransactionDto>> Handle(GetRecurringTransactionsQuery request, CancellationToken ct)
    {
        var query = _context.RecurringTransactions.AsQueryable();

        if (request.AccountId.HasValue)
            query = query.Where(r => r.AccountId == request.AccountId.Value);

        var items = await query
            .OrderBy(r => r.NextOccurrence)
            .ThenBy(r => r.Payee)
            .ToListAsync(ct);

        return items.Select(r => new RecurringTransactionDto(
            r.Id, r.AccountId, r.BucketId, r.Payee, r.AmountMilliunits,
            r.Memo, r.Frequency, r.StartDate, r.EndDate, r.NextOccurrence,
            r.CreatedAt, r.UpdatedAt)).ToList();
    }
}
