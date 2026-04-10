using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BucketBudget.Client;

public class BucketBudgetClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public BucketBudgetClient(string baseUrl, string? token = null)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // Auth
    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("auth/login", new LoginRequest(email, password), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct))!;
    }

    // Accounts
    public async Task<List<AccountDto>> GetAccountsAsync(bool includeClosed = false, CancellationToken ct = default)
    {
        var url = $"accounts?includeClosed={includeClosed}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<AccountDto>>(JsonOptions, ct))!;
    }

    // Transactions
    public async Task<List<TransactionDto>> GetTransactionsAsync(
        Guid? accountId = null,
        Guid? bucketId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (accountId.HasValue) qs.Add($"accountId={accountId}");
        if (bucketId.HasValue) qs.Add($"bucketId={bucketId}");
        if (from.HasValue) qs.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) qs.Add($"to={to:yyyy-MM-dd}");
        var url = "transactions" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<TransactionDto>>(JsonOptions, ct))!;
    }

    public async Task<Guid> CreateTransactionAsync(CreateTransactionRequest req, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("transactions", req, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
        return result.GetProperty("id").GetGuid();
    }

    public async Task<List<Guid>> BulkCreateTransactionsAsync(List<CreateTransactionRequest> transactions, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("transactions/bulk", new { Transactions = transactions }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<Guid>>(JsonOptions, ct))!;
    }

    // Budget
    public async Task<MonthBudgetDto> GetMonthBudgetAsync(string month, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"months/{month}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MonthBudgetDto>(JsonOptions, ct))!;
    }

    public async Task AssignBudgetAsync(string month, Guid bucketId, long allocatedMilliunits, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"months/{month}/buckets/{bucketId}",
            new UpsertAllocationRequest(allocatedMilliunits),
            ct);
        response.EnsureSuccessStatusCode();
    }

    // Exchange Rates
    public async Task PollRatesAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("exchange-rates/poll", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ExchangeRateDto>> GetLatestRatesAsync(string from, string to, string? type = null, CancellationToken ct = default)
    {
        var url = $"exchange-rates/latest?from={from}&to={to}";
        if (type is not null) url += $"&type={type}";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ExchangeRateDto>>(JsonOptions, ct))!;
    }

    // Recurring Transactions
    public async Task<List<RecurringTransactionDto>> GetRecurringTransactionsAsync(Guid? accountId = null, CancellationToken ct = default)
    {
        var url = "recurring-transactions" + (accountId.HasValue ? $"?accountId={accountId}" : "");
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<RecurringTransactionDto>>(JsonOptions, ct))!;
    }

    public async Task<RunRecurringResult> RunRecurringTransactionsAsync(DateOnly? asOf = null, CancellationToken ct = default)
    {
        var url = "recurring-transactions/run" + (asOf.HasValue ? $"?asOf={asOf:yyyy-MM-dd}" : "");
        var response = await _http.PostAsync(url, null, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RunRecurringResult>(JsonOptions, ct))!;
    }
}
