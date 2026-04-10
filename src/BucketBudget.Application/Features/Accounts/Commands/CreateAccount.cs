using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BucketBudget.Application.Features.Accounts.Commands;

public record CreateAccountCommand(string Name, string CurrencyCode) : IRequest<Guid>;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
    }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IBudgetDbContext _context;
    public CreateAccountCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken ct)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CurrencyCode = request.CurrencyCode.ToUpperInvariant(),
            BalanceMilliunits = 0
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(ct);
        return account.Id;
    }
}
