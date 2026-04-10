using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Transactions.Commands;

public record UpdateTransactionCommand(Guid Id, Guid? BucketId, string Payee, long AmountMilliunits, DateOnly Date, string? Memo, bool IsCleared) : IRequest;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Payee).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand>
{
    private readonly IBudgetDbContext _context;
    public UpdateTransactionCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task Handle(UpdateTransactionCommand request, CancellationToken ct)
    {
        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Transaction), request.Id);

        transaction.BucketId = request.BucketId;
        transaction.Payee = request.Payee;
        transaction.AmountMilliunits = request.AmountMilliunits;
        transaction.Date = request.Date;
        transaction.Memo = request.Memo;
        transaction.IsCleared = request.IsCleared;
        await _context.SaveChangesAsync(ct);
    }
}
