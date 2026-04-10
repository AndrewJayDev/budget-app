using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Transactions.Commands;

public record DeleteTransactionCommand(Guid Id) : IRequest;

public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly IBudgetDbContext _context;
    public DeleteTransactionCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task Handle(DeleteTransactionCommand request, CancellationToken ct)
    {
        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Transaction), request.Id);

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync(ct);
    }
}
