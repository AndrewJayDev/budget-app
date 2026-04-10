using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.RecurringTransactions.Commands;

public record DeleteRecurringTransactionCommand(Guid Id) : IRequest;

public class DeleteRecurringTransactionCommandHandler : IRequestHandler<DeleteRecurringTransactionCommand>
{
    private readonly IBudgetDbContext _context;
    public DeleteRecurringTransactionCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task Handle(DeleteRecurringTransactionCommand request, CancellationToken ct)
    {
        var recurring = await _context.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(RecurringTransaction), request.Id);

        _context.RecurringTransactions.Remove(recurring);
        await _context.SaveChangesAsync(ct);
    }
}
