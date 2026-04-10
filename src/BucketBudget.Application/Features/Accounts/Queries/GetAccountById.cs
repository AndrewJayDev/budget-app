using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Accounts.Queries;

public record GetAccountByIdQuery(Guid Id) : IRequest<AccountDto>;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    private readonly IBudgetDbContext _context;
    public GetAccountByIdQueryHandler(IBudgetDbContext context) => _context = context;

    public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken ct)
    {
        var account = await _context.Accounts
            .Where(a => a.Id == request.Id)
            .Select(a => new AccountDto(a.Id, a.Name, a.CurrencyCode, a.BalanceMilliunits, a.IsClosed, a.CreatedAt, a.UpdatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Account), request.Id);

        return account;
    }
}
