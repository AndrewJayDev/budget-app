using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Transactions.Commands;

public record BulkUpdateTransactionItem(Guid Id, Guid? BucketId, string Payee, long AmountMilliunits, DateOnly Date, string? Memo, bool IsCleared);

public record BulkUpdateTransactionsCommand(List<BulkUpdateTransactionItem> Transactions) : IRequest;

public class BulkUpdateTransactionsCommandValidator : AbstractValidator<BulkUpdateTransactionsCommand>
{
    public BulkUpdateTransactionsCommandValidator()
    {
        RuleFor(x => x.Transactions).NotEmpty();
        RuleForEach(x => x.Transactions).ChildRules(t =>
        {
            t.RuleFor(x => x.Id).NotEmpty();
            t.RuleFor(x => x.Payee).NotEmpty().MaximumLength(500);
            t.RuleFor(x => x.Date).NotEmpty();
        });
    }
}

public class BulkUpdateTransactionsCommandHandler : IRequestHandler<BulkUpdateTransactionsCommand>
{
    private readonly IBudgetDbContext _context;
    public BulkUpdateTransactionsCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task Handle(BulkUpdateTransactionsCommand request, CancellationToken ct)
    {
        var ids = request.Transactions.Select(t => t.Id).ToList();
        var transactions = await _context.Transactions.Where(t => ids.Contains(t.Id)).ToListAsync(ct);

        var missing = ids.Except(transactions.Select(t => t.Id)).FirstOrDefault();
        if (missing != Guid.Empty && !transactions.Any(t => t.Id == missing))
            throw new NotFoundException(nameof(Domain.Entities.Transaction), missing);

        var lookup = transactions.ToDictionary(t => t.Id);
        foreach (var item in request.Transactions)
        {
            if (!lookup.TryGetValue(item.Id, out var t)) continue;
            t.BucketId = item.BucketId;
            t.Payee = item.Payee;
            t.AmountMilliunits = item.AmountMilliunits;
            t.Date = item.Date;
            t.Memo = item.Memo;
            t.IsCleared = item.IsCleared;
        }

        await _context.SaveChangesAsync(ct);
    }
}
