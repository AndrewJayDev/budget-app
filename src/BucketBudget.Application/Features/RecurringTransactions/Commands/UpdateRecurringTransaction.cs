using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using BucketBudget.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.RecurringTransactions.Commands;

public record UpdateRecurringTransactionCommand(
    Guid Id,
    Guid? BucketId,
    string Payee,
    long AmountMilliunits,
    string? Memo,
    RecurrenceFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool AutoPost
) : IRequest;

public class UpdateRecurringTransactionCommandValidator : AbstractValidator<UpdateRecurringTransactionCommand>
{
    public UpdateRecurringTransactionCommandValidator()
    {
        RuleFor(x => x.Payee).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}

public class UpdateRecurringTransactionCommandHandler : IRequestHandler<UpdateRecurringTransactionCommand>
{
    private readonly IBudgetDbContext _context;
    public UpdateRecurringTransactionCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task Handle(UpdateRecurringTransactionCommand request, CancellationToken ct)
    {
        var recurring = await _context.RecurringTransactions.FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(RecurringTransaction), request.Id);

        recurring.BucketId = request.BucketId;
        recurring.Payee = request.Payee;
        recurring.AmountMilliunits = request.AmountMilliunits;
        recurring.Memo = request.Memo;
        recurring.Frequency = request.Frequency;
        recurring.StartDate = request.StartDate;
        recurring.EndDate = request.EndDate;
        recurring.AutoPost = request.AutoPost;

        await _context.SaveChangesAsync(ct);
    }
}
