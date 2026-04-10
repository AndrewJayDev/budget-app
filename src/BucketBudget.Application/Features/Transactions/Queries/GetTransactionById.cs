using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Transactions.Queries;

public record GetTransactionByIdQuery(Guid Id) : IRequest<TransactionDto>;

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly IBudgetDbContext _context;
    public GetTransactionByIdQueryHandler(IBudgetDbContext context) => _context = context;

    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken ct)
    {
        return await _context.Transactions
            .Where(t => t.Id == request.Id)
            .Select(t => new TransactionDto(t.Id, t.AccountId, t.BucketId, t.Payee, t.AmountMilliunits, t.Date, t.Memo, t.IsCleared, t.CreatedAt, t.UpdatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Transaction), request.Id);
    }
}
