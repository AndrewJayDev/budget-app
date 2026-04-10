using BucketBudget.Application.Common.Exceptions;
using BucketBudget.Application.Features.Accounts.Commands;
using BucketBudget.Application.Features.Accounts.Queries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BucketBudget.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts").WithTags("Accounts").RequireAuthorization();

        group.MapGet("/", async (ISender sender, [FromQuery] bool includeClosed = false, CancellationToken ct = default) =>
            Results.Ok(await sender.Send(new GetAccountsQuery(includeClosed), ct)));

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            try { return Results.Ok(await sender.Send(new GetAccountByIdQuery(id), ct)); }
            catch (NotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/", async ([FromBody] CreateAccountCommand command, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/accounts/{id}", new { id });
            }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateAccountRequest req, ISender sender, CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new UpdateAccountCommand(id, req.Name, req.CurrencyCode, req.IsClosed), ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
            catch (ValidationException ex) { return Results.ValidationProblem(ex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())); }
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new DeleteAccountCommand(id), ct);
                return Results.NoContent();
            }
            catch (NotFoundException) { return Results.NotFound(); }
        });

        return app;
    }
}

public record UpdateAccountRequest(string Name, string CurrencyCode, bool IsClosed);
