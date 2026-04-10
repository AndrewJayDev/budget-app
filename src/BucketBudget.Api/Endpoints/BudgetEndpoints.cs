using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Features.Budget.Commands;
using BucketBudget.Application.Features.Budget.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BucketBudget.Api.Endpoints;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var months = app.MapGroup("/months").WithTags("Budget").RequireAuthorization();

        months.MapGet("/{month}", async (string month, ISender sender, CancellationToken ct) =>
        {
            if (!TryParseYearMonth(month, out var year, out var mon))
                return Results.BadRequest("Month must be in YYYY-MM format.");
            return Results.Ok(await sender.Send(new GetMonthBudgetQuery(year, mon), ct));
        });

        months.MapPut("/{month}/buckets/{bucketId:guid}", async (string month, Guid bucketId, [FromBody] UpsertAllocationRequest req, ISender sender, CancellationToken ct) =>
        {
            if (!TryParseYearMonth(month, out var year, out var mon))
                return Results.BadRequest("Month must be in YYYY-MM format.");
            try
            {
                await sender.Send(new UpsertBucketAllocationCommand(bucketId, year, mon, req.AllocatedMilliunits), ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        return app;
    }

    private static bool TryParseYearMonth(string value, out int year, out int month)
    {
        year = 0; month = 0;
        if (value.Length != 7 || value[4] != '-') return false;
        return int.TryParse(value[..4], out year) && int.TryParse(value[5..], out month) && month >= 1 && month <= 12;
    }
}

public record UpsertAllocationRequest(long AllocatedMilliunits);
