using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Budget.Commands;

public record UpsertBucketAllocationCommand(Guid BucketId, int Year, int Month, long AllocatedMilliunits) : IRequest;

public class UpsertBucketAllocationCommandValidator : AbstractValidator<UpsertBucketAllocationCommand>
{
    public UpsertBucketAllocationCommandValidator()
    {
        RuleFor(x => x.BucketId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class UpsertBucketAllocationCommandHandler : IRequestHandler<UpsertBucketAllocationCommand>
{
    private readonly IBudgetDbContext _context;
    public UpsertBucketAllocationCommandHandler(IBudgetDbContext context) => _context = context;

    public async Task Handle(UpsertBucketAllocationCommand request, CancellationToken ct)
    {
        var bucketExists = await _context.Buckets.AnyAsync(b => b.Id == request.BucketId, ct);
        if (!bucketExists) throw new NotFoundException(nameof(Bucket), request.BucketId);

        var allocation = await _context.MonthlyBucketAllocations
            .FirstOrDefaultAsync(a => a.BucketId == request.BucketId && a.Year == request.Year && a.Month == request.Month, ct);

        if (allocation is null)
        {
            allocation = new MonthlyBucketAllocation
            {
                Id = Guid.NewGuid(),
                BucketId = request.BucketId,
                Year = request.Year,
                Month = request.Month,
                AllocatedMilliunits = request.AllocatedMilliunits
            };
            _context.MonthlyBucketAllocations.Add(allocation);
        }
        else
        {
            allocation.AllocatedMilliunits = request.AllocatedMilliunits;
        }

        await _context.SaveChangesAsync(ct);
    }
}
