using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Features.Transactions.Commands;
using BucketBudget.Application.Features.Transactions.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BucketBudget.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/transactions").WithTags("Transactions").RequireAuthorization();

        group.MapGet("/", async (ISender sender, [FromQuery] Guid? accountId, [FromQuery] Guid? bucketId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetTransactionsQuery(accountId, bucketId, from, to), ct)));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            try { return Results.Ok(await sender.Send(new GetTransactionByIdQuery(id), ct)); }
            catch (NotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/", async ([FromBody] CreateTransactionCommand command, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/transactions/{id}", new { id });
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        group.MapPost("/bulk", async ([FromBody] BulkCreateTransactionsCommand command, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var ids = await sender.Send(command, ct);
                return Results.Ok(ids);
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateTransactionRequest req, ISender sender, CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new UpdateTransactionCommand(id, req.BucketId, req.Payee, req.AmountMilliunits, req.Date, req.Memo, req.IsCleared), ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        group.MapPut("/bulk", async ([FromBody] BulkUpdateTransactionsCommand command, ISender sender, CancellationToken ct) =>
        {
            try
            {
                await sender.Send(command, ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new DeleteTransactionCommand(id), ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
        });

        return app;
    }
}

public record UpdateTransactionRequest(Guid? BucketId, string Payee, long AmountMilliunits, DateOnly Date, string? Memo, bool IsCleared);
