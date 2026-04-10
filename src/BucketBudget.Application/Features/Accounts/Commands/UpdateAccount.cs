using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Accounts.Commands;

public record UpdateAccountCommand(Guid Id, string Name, string CurrencyCode, bool IsClosed) : IRequest;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
    }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand>
{
    private readonly IBudgetDbContext _context;
    public UpdateAccountCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task Handle(UpdateAccountCommand request, CancellationToken ct)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Account), request.Id);

        account.Name = request.Name;
        account.CurrencyCode = request.CurrencyCode.ToUpperInvariant();
        account.IsClosed = request.IsClosed;
        await _context.SaveChangesAsync(ct);
    }
}
