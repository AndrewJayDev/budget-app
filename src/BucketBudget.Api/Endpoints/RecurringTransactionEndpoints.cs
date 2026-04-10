using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Features.RecurringTransactions.Commands;
using BucketBudget.Application.Features.RecurringTransactions.Queries;
using BucketBudget.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BucketBudget.Api.Endpoints;

public static class RecurringTransactionEndpoints
{
    public static IEndpointRouteBuilder MapRecurringTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/recurring-transactions").WithTags("RecurringTransactions").RequireAuthorization();

        group.MapGet("/", async (ISender sender, [FromQuery] Guid? accountId, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetRecurringTransactionsQuery(accountId), ct)));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            try { return Results.Ok(await sender.Send(new GetRecurringTransactionByIdQuery(id), ct)); }
            catch (NotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/", async ([FromBody] CreateRecurringTransactionRequest req, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var id = await sender.Send(new CreateRecurringTransactionCommand(
                    req.AccountId, req.BucketId, req.Payee, req.AmountMilliunits,
                    req.Memo, req.Frequency, req.StartDate, req.EndDate, req.AutoPost), ct);
                return Results.Created($"/recurring-transactions/{id}", new { id });
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateRecurringTransactionRequest req, ISender sender, CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new UpdateRecurringTransactionCommand(
                    id, req.BucketId, req.Payee, req.AmountMilliunits,
                    req.Memo, req.Frequency, req.StartDate, req.EndDate, req.AutoPost), ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new DeleteRecurringTransactionCommand(id), ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
        });

        return app;
    }
}

public record CreateRecurringTransactionRequest(
    Guid AccountId,
    Guid? BucketId,
    string Payee,
    long AmountMilliunits,
    string? Memo,
    RecurrenceFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool AutoPost
);

public record UpdateRecurringTransactionRequest(
    Guid? BucketId,
    string Payee,
    long AmountMilliunits,
    string? Memo,
    RecurrenceFrequency Frequency,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool AutoPost
);
