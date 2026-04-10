using BucketBudget.Api.Endpoints;
using BucketBudget.Application;
using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Entities;
using BucketBudget.Domain.Enums;
using BucketBudget.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("healthy"));
app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapTransactionEndpoints();
app.MapBudgetEndpoints();
app.MapRecurringTransactionEndpoints();

// --- Exchange Rate Endpoints ---

// GET /exchange-rates/latest?from=USD&to=ARS&type=Official
app.MapGet("/exchange-rates/latest", async (
    string from,
    string to,
    ExchangeRateType? type,
    IBudgetDbContext db,
    CancellationToken ct) =>
{
    var query = db.ExchangeRates
        .Where(r => r.FromCurrencyCode == from.ToUpperInvariant()
                    && r.ToCurrencyCode == to.ToUpperInvariant());

    if (type.HasValue)
        query = query.Where(r => r.RateType == type.Value);

    var latest = await query
        .OrderByDescending(r => r.EffectiveDate)
        .ThenByDescending(r => r.CreatedAt)
        .Take(10)
        .ToListAsync(ct);

    return latest.Any() ? Results.Ok(latest) : Results.NotFound();
});

// POST /exchange-rates/poll — manually trigger a rate poll
app.MapPost("/exchange-rates/poll", async (IExchangeRatePoller poller, CancellationToken ct) =>
{
    await poller.PollAsync(ct);
    return Results.Ok();
});

// --- Net Worth ---

// GET /net-worth?currency=USD
app.MapGet("/net-worth", async (
    string currency,
    ExchangeRateType rateType,
    IBudgetDbContext db,
    CancellationToken ct) =>
{
    var accounts = await db.Accounts
        .Where(a => !a.IsClosed)
        .ToListAsync(ct);

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var targetCurrency = currency.ToUpperInvariant();

    // Gather all distinct currency codes from accounts
    var currencyCodes = accounts.Select(a => a.CurrencyCode).Distinct().ToList();

    // Fetch latest rates for each currency pair we need
    var ratesNeeded = currencyCodes.Where(c => c != targetCurrency).ToList();
    var rateDict = new Dictionary<string, decimal>();

    foreach (var fromCurrency in ratesNeeded)
    {
        // Look for from->target or target->from rates
        var rate = await db.ExchangeRates
            .Where(r => r.FromCurrencyCode == fromCurrency && r.ToCurrencyCode == targetCurrency && r.RateType == rateType)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        if (rate != null)
        {
            rateDict[fromCurrency] = rate.Rate;
            continue;
        }

        // Try inverse
        var inverse = await db.ExchangeRates
            .Where(r => r.FromCurrencyCode == targetCurrency && r.ToCurrencyCode == fromCurrency && r.RateType == rateType)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        if (inverse != null)
            rateDict[fromCurrency] = 1m / inverse.Rate;
    }

    var totalMilliunits = 0L;
    var missingRates = new List<string>();

    foreach (var account in accounts)
    {
        if (account.CurrencyCode == targetCurrency)
        {
            totalMilliunits += account.BalanceMilliunits;
        }
        else if (rateDict.TryGetValue(account.CurrencyCode, out var rate))
        {
            totalMilliunits += (long)(account.BalanceMilliunits * rate);
        }
        else
        {
            missingRates.Add(account.CurrencyCode);
        }
    }

    return Results.Ok(new
    {
        Currency = targetCurrency,
        RateType = rateType,
        TotalMilliunits = totalMilliunits,
        Total = totalMilliunits / 1000m,
        MissingRates = missingRates
    });
});

// --- Cross-Currency Transfer ---

// POST /transfers/cross-currency
app.MapPost("/transfers/cross-currency", async (
    CrossCurrencyTransferRequest request,
    IBudgetDbContext db,
    CancellationToken ct) =>
{
    var sourceAccount = await db.Accounts.FindAsync([request.SourceAccountId], ct);
    if (sourceAccount is null) return Results.NotFound($"Source account {request.SourceAccountId} not found");

    var destAccount = await db.Accounts.FindAsync([request.DestinationAccountId], ct);
    if (destAccount is null) return Results.NotFound($"Destination account {request.DestinationAccountId} not found");

    // Fetch or look up the exchange rate
    ExchangeRate? exchangeRate = null;
    if (request.ExchangeRateId.HasValue)
    {
        exchangeRate = await db.ExchangeRates.FindAsync([request.ExchangeRateId.Value], ct);
        if (exchangeRate is null) return Results.NotFound($"Exchange rate {request.ExchangeRateId} not found");
    }
    else if (request.RateType.HasValue)
    {
        // Use the latest rate
        exchangeRate = await db.ExchangeRates
            .Where(r => r.FromCurrencyCode == sourceAccount.CurrencyCode
                        && r.ToCurrencyCode == destAccount.CurrencyCode
                        && r.RateType == request.RateType.Value)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct);

        if (exchangeRate is null)
            return Results.UnprocessableEntity($"No {request.RateType} exchange rate found for {sourceAccount.CurrencyCode}->{destAccount.CurrencyCode}");
    }

    var pairId = Guid.NewGuid();
    var today = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

    // Debit source
    var sourceTransaction = new Transaction
    {
        Id = Guid.NewGuid(),
        AccountId = request.SourceAccountId,
        Payee = request.Payee,
        AmountMilliunits = -Math.Abs(request.SourceAmountMilliunits),
        Date = today,
        Memo = request.Memo,
        IsCleared = request.IsCleared,
        TransferPairId = pairId,
        ExchangeRateId = exchangeRate?.Id
    };

    // Credit destination
    var destTransaction = new Transaction
    {
        Id = Guid.NewGuid(),
        AccountId = request.DestinationAccountId,
        Payee = request.Payee,
        AmountMilliunits = Math.Abs(request.DestinationAmountMilliunits),
        Date = today,
        Memo = request.Memo,
        IsCleared = request.IsCleared,
        TransferPairId = pairId,
        ExchangeRateId = exchangeRate?.Id
    };

    // Update account balances
    sourceAccount.BalanceMilliunits += sourceTransaction.AmountMilliunits;
    destAccount.BalanceMilliunits += destTransaction.AmountMilliunits;

    db.Transactions.Add(sourceTransaction);
    db.Transactions.Add(destTransaction);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/transfers/{pairId}", new
    {
        TransferPairId = pairId,
        SourceTransactionId = sourceTransaction.Id,
        DestinationTransactionId = destTransaction.Id,
        ExchangeRateId = exchangeRate?.Id,
        Rate = exchangeRate?.Rate
    });
});

app.Run();

record CrossCurrencyTransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    long SourceAmountMilliunits,
    long DestinationAmountMilliunits,
    string Payee,
    string? Memo,
    bool IsCleared,
    DateOnly? Date,
    Guid? ExchangeRateId,
    ExchangeRateType? RateType
);
