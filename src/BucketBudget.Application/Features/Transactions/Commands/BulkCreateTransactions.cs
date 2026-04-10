using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Transactions.Commands;

public record BulkCreateTransactionItem(Guid AccountId, Guid? BucketId, string Payee, long AmountMilliunits, DateOnly Date, string? Memo, bool IsCleared);

public record BulkCreateTransactionsCommand(List<BulkCreateTransactionItem> Transactions) : IRequest<List<Guid>>;

public class BulkCreateTransactionsCommandValidator : AbstractValidator<BulkCreateTransactionsCommand>
{
    public BulkCreateTransactionsCommandValidator()
    {
        RuleFor(x => x.Transactions).NotEmpty();
        RuleForEach(x => x.Transactions).ChildRules(t =>
        {
            t.RuleFor(x => x.AccountId).NotEmpty();
            t.RuleFor(x => x.Payee).NotEmpty().MaximumLength(500);
            t.RuleFor(x => x.Date).NotEmpty();
        });
    }
}

public class BulkCreateTransactionsCommandHandler : IRequestHandler<BulkCreateTransactionsCommand, List<Guid>>
{
    private readonly IBudgetDbContext _context;
    public BulkCreateTransactionsCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task<List<Guid>> Handle(BulkCreateTransactionsCommand request, CancellationToken ct)
    {
        var accountIds = request.Transactions.Select(t => t.AccountId).Distinct().ToList();
        var existingAccountIds = await _context.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(ct);

        var missing = accountIds.Except(existingAccountIds).FirstOrDefault();
        if (missing != Guid.Empty && !existingAccountIds.Contains(missing))
            throw new NotFoundException(nameof(Account), missing);

        var transactions = request.Transactions.Select(item => new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = item.AccountId,
            BucketId = item.BucketId,
            Payee = item.Payee,
            AmountMilliunits = item.AmountMilliunits,
            Date = item.Date,
            Memo = item.Memo,
            IsCleared = item.IsCleared
        }).ToList();

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync(ct);
        return transactions.Select(t => t.Id).ToList();
    }
}
