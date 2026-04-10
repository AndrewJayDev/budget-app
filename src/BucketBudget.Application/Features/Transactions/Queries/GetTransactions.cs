using BucketBudget.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Transactions.Queries;

public record GetTransactionsQuery(Guid? AccountId = null, Guid? BucketId = null, DateOnly? From = null, DateOnly? To = null) : IRequest<List<TransactionDto>>;

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, List<TransactionDto>>
{
    private readonly IBudgetDbContext _context;
    public GetTransactionsQueryHandler(IBudgetDbContext context) => _context = context;

    public async Task<List<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken ct)
    {
        var query = _context.Transactions.AsQueryable();
        if (request.AccountId.HasValue) query = query.Where(t => t.AccountId == request.AccountId);
        if (request.BucketId.HasValue) query = query.Where(t => t.BucketId == request.BucketId);
        if (request.From.HasValue) query = query.Where(t => t.Date >= request.From);
        if (request.To.HasValue) query = query.Where(t => t.Date <= request.To);

        return await query
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionDto(t.Id, t.AccountId, t.BucketId, t.Payee, t.AmountMilliunits, t.Date, t.Memo, t.IsCleared, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct);
    }
}
