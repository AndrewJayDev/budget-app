using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Transactions.Commands;

public record CreateTransactionCommand(Guid AccountId, Guid? BucketId, string Payee, long AmountMilliunits, DateOnly Date, string? Memo, bool IsCleared) : IRequest<Guid>;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Payee).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Guid>
{
    private readonly IBudgetDbContext _context;
    public CreateTransactionCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateTransactionCommand request, CancellationToken ct)
    {
        var accountExists = await _context.Accounts.AnyAsync(a => a.Id == request.AccountId, ct);
        if (!accountExists) throw new NotFoundException(nameof(Account), request.AccountId);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            BucketId = request.BucketId,
            Payee = request.Payee,
            AmountMilliunits = request.AmountMilliunits,
            Date = request.Date,
            Memo = request.Memo,
            IsCleared = request.IsCleared
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(ct);
        return transaction.Id;
    }
}
