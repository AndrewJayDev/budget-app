using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.RecurringTransactions.Queries;

public record GetRecurringTransactionByIdQuery(Guid Id) : IRequest<RecurringTransactionDto>;

public class GetRecurringTransactionByIdQueryHandler : IRequestHandler<GetRecurringTransactionByIdQuery, RecurringTransactionDto>
{
    private readonly IBudgetDbContext _context;
    public GetRecurringTransactionByIdQueryHandler(IBudgetDbContext context) => _context = context;

    public async Task<RecurringTransactionDto> Handle(GetRecurringTransactionByIdQuery request, CancellationToken ct)
    {
        var r = await _context.RecurringTransactions.FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(RecurringTransaction), request.Id);

        return new RecurringTransactionDto(
            r.Id, r.AccountId, r.BucketId, r.Payee, r.AmountMilliunits,
            r.Memo, r.Frequency, r.StartDate, r.EndDate, r.NextOccurrence,
            r.AutoPost, r.CreatedAt, r.UpdatedAt);
    }
}
