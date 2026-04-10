using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Accounts.Commands;

public record DeleteAccountCommand(Guid Id) : IRequest;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IBudgetDbContext _context;
    public DeleteAccountCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Account), request.Id);

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync(ct);
    }
}
