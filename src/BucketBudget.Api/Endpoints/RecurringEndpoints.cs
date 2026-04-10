using BucketBudget.Application.Features.RecurringTransactions.Commands;
using BucketBudget.Application.Features.RecurringTransactions.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BucketBudget.Api.Endpoints;

public static class RecurringEndpoints
{
    public static IEndpointRouteBuilder MapRecurringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/recurring-transactions").WithTags("Recurring").RequireAuthorization();

        group.MapGet("/", async (ISender sender, [FromQuery] Guid? accountId, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetRecurringTransactionsQuery(accountId), ct)));

        group.MapPost("/run", async (ISender sender, [FromQuery] DateOnly? asOf, CancellationToken ct) =>
        {
            var result = await sender.Send(new RunRecurringTransactionsCommand(asOf), ct);
            return Results.Ok(result);
        });

        return app;
    }
}
