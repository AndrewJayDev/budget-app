using BucketBudget.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BucketBudget.Application.Features.Budget.Queries;

public record BucketSummaryDto(Guid Id, string Name, int SortOrder, long AllocatedMilliunits, long ActivityMilliunits, long AvailableMilliunits);
public record BucketGroupSummaryDto(Guid Id, string Name, int SortOrder, List<BucketSummaryDto> Buckets);
public record MonthBudgetDto(int Year, int Month, List<BucketGroupSummaryDto> BucketGroups);

public record GetMonthBudgetQuery(int Year, int Month) : IRequest<MonthBudgetDto>;

public class GetMonthBudgetQueryHandler : IRequestHandler<GetMonthBudgetQuery, MonthBudgetDto>
{
    private readonly IBudgetDbContext _context;
    public GetMonthBudgetQueryHandler(IBudgetDbContext context) => _context = context;

    public async Task<MonthBudgetDto> Handle(GetMonthBudgetQuery request, CancellationToken ct)
    {
        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var allocations = await _context.MonthlyBucketAllocations
            .Where(a => a.Year == request.Year && a.Month == request.Month)
            .ToDictionaryAsync(a => a.BucketId, a => a.AllocatedMilliunits, ct);

        var activity = await _context.Transactions
            .Where(t => t.BucketId != null && t.Date >= monthStart && t.Date <= monthEnd)
            .GroupBy(t => t.BucketId!.Value)
            .Select(g => new { BucketId = g.Key, Total = g.Sum(t => t.AmountMilliunits) })
            .ToDictionaryAsync(g => g.BucketId, g => g.Total, ct);

        var groups = await _context.BucketGroups
            .Include(g => g.Buckets)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(ct);

        var groupDtos = groups.Select(g => new BucketGroupSummaryDto(
            g.Id,
            g.Name,
            g.SortOrder,
            g.Buckets.OrderBy(b => b.SortOrder).Select(b =>
            {
                var allocated = allocations.GetValueOrDefault(b.Id, 0);
                var act = activity.GetValueOrDefault(b.Id, 0);
                return new BucketSummaryDto(b.Id, b.Name, b.SortOrder, allocated, act, allocated + act);
            }).ToList()
        )).ToList();

        return new MonthBudgetDto(request.Year, request.Month, groupDtos);
    }
}
