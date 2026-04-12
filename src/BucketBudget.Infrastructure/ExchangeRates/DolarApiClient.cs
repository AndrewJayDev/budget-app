using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BucketBudget.Domain.Entities;
using BucketBudget.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BucketBudget.Infrastructure.ExchangeRates;

public class DolarApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<DolarApiClient> _logger;

    public DolarApiClient(HttpClient http, ILogger<DolarApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExchangeRate>> FetchTodayRatesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rates = new List<ExchangeRate>();

        var endpoints = new[]
        {
            ("oficial", ExchangeRateType.Official),
            ("blue", ExchangeRateType.Blue),
            ("bolsa", ExchangeRateType.Mep),
            ("contadoconliqui", ExchangeRateType.Ccl),
            ("tarjeta", ExchangeRateType.Tarjeta),
            ("cripto", ExchangeRateType.Cripto),
        };

        foreach (var (casa, rateType) in endpoints)
        {
            try
            {
                var response = await _http.GetFromJsonAsync<DolarApiResponse>(
                    $"v1/dolares/{casa}", cancellationToken);

                if (response is null) continue;

                // dolarapi returns ARS per USD; we store as ARS/USD (how many ARS per 1 USD)
                // Use venta (sell) price as the reference rate
                rates.Add(new ExchangeRate
                {
                    Id = Guid.NewGuid(),
                    FromCurrencyCode = "USD",
                    ToCurrencyCode = "ARS",
                    Rate = response.Venta,
                    RateType = rateType,
                    EffectiveDate = today,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch {RateType} rate from dolarapi.com", rateType);
            }
        }

        return rates;
    }

    private sealed class DolarApiResponse
    {
        [JsonPropertyName("compra")]
        public decimal Compra { get; set; }

        [JsonPropertyName("venta")]
        public decimal Venta { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }
}
