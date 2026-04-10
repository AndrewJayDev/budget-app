using BucketBudget.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Accounts.Queries;

public record GetAccountsQuery(bool IncludeClosed = false) : IRequest<List<AccountDto>>;

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, List<AccountDto>>
{
    private readonly IBudgetDbContext _context;
    public GetAccountsQueryHandler(IBudgetDbContext context) => _context = context;

    public async Task<List<AccountDto>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        var query = _context.Accounts.AsQueryable();
        if (!request.IncludeClosed)
            query = query.Where(a => !a.IsClosed);

        return await query
            .OrderBy(a => a.Name)
            .Select(a => new AccountDto(a.Id, a.Name, a.CurrencyCode, a.BalanceMilliunits, a.IsClosed, a.CreatedAt, a.UpdatedAt))
            .ToListAsync(ct);
    }
}
