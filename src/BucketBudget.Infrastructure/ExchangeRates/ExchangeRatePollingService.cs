using BucketBudget.Application.Interfaces;
using BucketBudget.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BucketBudget.Infrastructure.ExchangeRates;

public class ExchangeRatePollingService : BackgroundService, IExchangeRatePoller
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExchangeRatePollingService> _logger;

    public ExchangeRatePollingService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExchangeRatePollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once at startup, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            await PollAsync(stoppingToken);

            // Wait until the next day (poll once daily)
            var nextRun = DateTime.UtcNow.Date.AddDays(1);
            var delay = nextRun - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);
        }
    }

    public async Task PollAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Polling exchange rates from dolarapi.com");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IBudgetDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<DolarApiClient>();

        var rates = await client.FetchTodayRatesAsync(cancellationToken);

        foreach (var rate in rates)
        {
            var exists = await db.ExchangeRates.AnyAsync(
                r => r.FromCurrencyCode == rate.FromCurrencyCode
                     && r.ToCurrencyCode == rate.ToCurrencyCode
                     && r.RateType == rate.RateType
                     && r.EffectiveDate == rate.EffectiveDate,
                cancellationToken);

            if (!exists)
            {
                db.ExchangeRates.Add(rate);
                _logger.LogInformation(
                    "Stored {RateType} rate: 1 {From} = {Rate} {To} on {Date}",
                    rate.RateType, rate.FromCurrencyCode, rate.Rate, rate.ToCurrencyCode, rate.EffectiveDate);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
