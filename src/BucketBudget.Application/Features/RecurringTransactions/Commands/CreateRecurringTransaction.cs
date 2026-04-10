using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using BucketBudget.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.RecurringTransactions.Commands;

public record CreateRecurringTransactionCommand(
    Guid AccountId,
    Guid? BucketId,
    string Payee,
    long AmountMilliunits,
    string? Memo,
    RecurrenceFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool AutoPost
) : IRequest<Guid>;

public class CreateRecurringTransactionCommandValidator : AbstractValidator<CreateRecurringTransactionCommand>
{
    public CreateRecurringTransactionCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Payee).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}

public class CreateRecurringTransactionCommandHandler : IRequestHandler<CreateRecurringTransactionCommand, Guid>
{
    private readonly IBudgetDbContext _context;
    public CreateRecurringTransactionCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateRecurringTransactionCommand request, CancellationToken ct)
    {
        var accountExists = await _context.Accounts.AnyAsync(a => a.Id == request.AccountId, ct);
        if (!accountExists) throw new NotFoundException(nameof(Account), request.AccountId);

        var recurring = new RecurringTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            BucketId = request.BucketId,
            Payee = request.Payee,
            AmountMilliunits = request.AmountMilliunits,
            Memo = request.Memo,
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            NextOccurrence = request.StartDate,
            AutoPost = request.AutoPost
        };

        _context.RecurringTransactions.Add(recurring);
        await _context.SaveChangesAsync(ct);
        return recurring.Id;
    }
}
