using BucketBudget.Cli;
using BucketBudget.Client;
using System.CommandLine;
using System.Globalization;

var rootCommand = new RootCommand("BucketBudget CLI — manage budgets, accounts, and transactions");

// ─── login ───────────────────────────────────────────────────────────────────
var loginCmd = new Command("login", "Authenticate and store credentials");
var loginEmail = new Option<string>("--email", "Email address") { IsRequired = true };
var loginPassword = new Option<string>("--password", "Password") { IsRequired = true };
loginCmd.AddOption(loginEmail);
loginCmd.AddOption(loginPassword);
loginCmd.SetHandler(async (string email, string password) =>
{
    var config = CliConfig.Load();
    var client = new BucketBudgetClient(config.BaseUrl);
    var result = await client.LoginAsync(email, password);
    config.Token = result.Token;
    config.Save();
    Console.WriteLine($"Logged in as {result.Email}");
}, loginEmail, loginPassword);
rootCommand.AddCommand(loginCmd);

// ─── accounts ─────────────────────────────────────────────────────────────────
var accountsCmd = new Command("accounts", "Manage accounts");

var accountsListCmd = new Command("list", "List all accounts");
var includeClosedOpt = new Option<bool>("--include-closed", "Include closed accounts");
accountsListCmd.AddOption(includeClosedOpt);
accountsListCmd.SetHandler(async (bool includeClosed) =>
{
    var client = GetClient();
    var accounts = await client.GetAccountsAsync(includeClosed);
    if (accounts.Count == 0) { Console.WriteLine("No accounts found."); return; }
    Console.WriteLine($"{"ID",-36}  {"Name",-30}  {"Currency",-8}  {"Balance",12}  {"Closed"}");
    Console.WriteLine(new string('-', 100));
    foreach (var a in accounts)
        Console.WriteLine($"{a.Id,-36}  {a.Name,-30}  {a.CurrencyCode,-8}  {a.BalanceMilliunits / 1000m,12:F2}  {(a.IsClosed ? "yes" : "")}");
}, includeClosedOpt);
accountsCmd.AddCommand(accountsListCmd);
rootCommand.AddCommand(accountsCmd);

// ─── transactions ─────────────────────────────────────────────────────────────
var txCmd = new Command("transactions", "Manage transactions");

var txListCmd = new Command("list", "List transactions");
var txAccountId = new Option<Guid?>("--account-id", "Filter by account ID");
var txBucketId = new Option<Guid?>("--bucket-id", "Filter by bucket ID");
var txFrom = new Option<string?>("--from", "Start date (YYYY-MM-DD)");
var txTo = new Option<string?>("--to", "End date (YYYY-MM-DD)");
txListCmd.AddOption(txAccountId);
txListCmd.AddOption(txBucketId);
txListCmd.AddOption(txFrom);
txListCmd.AddOption(txTo);
txListCmd.SetHandler(async (Guid? accountId, Guid? bucketId, string? from, string? to) =>
{
    var client = GetClient();
    DateOnly? fromDate = from is not null ? DateOnly.ParseExact(from, "yyyy-MM-dd") : null;
    DateOnly? toDate = to is not null ? DateOnly.ParseExact(to, "yyyy-MM-dd") : null;
    var txs = await client.GetTransactionsAsync(accountId, bucketId, fromDate, toDate);
    if (txs.Count == 0) { Console.WriteLine("No transactions found."); return; }
    Console.WriteLine($"{"ID",-36}  {"Date",-10}  {"Payee",-30}  {"Amount",12}  {"Cleared"}");
    Console.WriteLine(new string('-', 100));
    foreach (var t in txs)
        Console.WriteLine($"{t.Id,-36}  {t.Date,-10}  {t.Payee,-30}  {t.AmountMilliunits / 1000m,12:F2}  {(t.IsCleared ? "yes" : "")}");
}, txAccountId, txBucketId, txFrom, txTo);
txCmd.AddCommand(txListCmd);

var txCreateCmd = new Command("create", "Create a transaction");
var txCreateAccount = new Option<Guid>("--account-id", "Account ID") { IsRequired = true };
var txCreatePayee = new Option<string>("--payee", "Payee name") { IsRequired = true };
var txCreateAmount = new Option<decimal>("--amount", "Amount (e.g. -50.00 for expense)") { IsRequired = true };
var txCreateDate = new Option<string>("--date", "Date (YYYY-MM-DD), defaults to today");
var txCreateMemo = new Option<string?>("--memo", "Memo");
var txCreateCleared = new Option<bool>("--cleared", "Mark as cleared");
var txCreateBucket = new Option<Guid?>("--bucket-id", "Bucket ID");
txCreateCmd.AddOption(txCreateAccount);
txCreateCmd.AddOption(txCreatePayee);
txCreateCmd.AddOption(txCreateAmount);
txCreateCmd.AddOption(txCreateDate);
txCreateCmd.AddOption(txCreateMemo);
txCreateCmd.AddOption(txCreateCleared);
txCreateCmd.AddOption(txCreateBucket);
txCreateCmd.SetHandler(async (Guid accountId, string payee, decimal amount, string? date, string? memo, bool cleared, Guid? bucketId) =>
{
    var client = GetClient();
    var txDate = date is not null
        ? DateOnly.ParseExact(date, "yyyy-MM-dd")
        : DateOnly.FromDateTime(DateTime.UtcNow);
    var id = await client.CreateTransactionAsync(new CreateTransactionRequest(
        accountId, bucketId, payee, (long)(amount * 1000), txDate, memo, cleared));
    Console.WriteLine($"Created transaction {id}");
}, txCreateAccount, txCreatePayee, txCreateAmount, txCreateDate, txCreateMemo, txCreateCleared, txCreateBucket);
txCmd.AddCommand(txCreateCmd);

var txImportCmd = new Command("import", "Import transactions from CSV");
var txImportAccount = new Option<Guid>("--account-id", "Account ID") { IsRequired = true };
var txImportFile = new Option<FileInfo>("--file", "CSV file path") { IsRequired = true };
txImportCmd.AddOption(txImportAccount);
txImportCmd.AddOption(txImportFile);
txImportCmd.SetHandler(async (Guid accountId, FileInfo file) =>
{
    if (!file.Exists) { Console.Error.WriteLine($"File not found: {file.FullName}"); return; }
    var client = GetClient();
    var rows = ParseCsv(file.FullName, accountId);
    if (rows.Count == 0) { Console.WriteLine("No transactions to import."); return; }
    var ids = await client.BulkCreateTransactionsAsync(rows);
    Console.WriteLine($"Imported {ids.Count} transaction(s).");
}, txImportAccount, txImportFile);
txCmd.AddCommand(txImportCmd);

rootCommand.AddCommand(txCmd);

// ─── budget ───────────────────────────────────────────────────────────────────
var budgetCmd = new Command("budget", "Manage budget");

var budgetAssignCmd = new Command("assign", "Assign amount to a bucket for a month");
var budgetMonth = new Option<string>("--month", "Month in YYYY-MM format") { IsRequired = true };
var budgetBucketId = new Option<Guid>("--bucket-id", "Bucket ID") { IsRequired = true };
var budgetAmount = new Option<decimal>("--amount", "Amount to allocate") { IsRequired = true };
budgetAssignCmd.AddOption(budgetMonth);
budgetAssignCmd.AddOption(budgetBucketId);
budgetAssignCmd.AddOption(budgetAmount);
budgetAssignCmd.SetHandler(async (string month, Guid bucketId, decimal amount) =>
{
    var client = GetClient();
    await client.AssignBudgetAsync(month, bucketId, (long)(amount * 1000));
    Console.WriteLine($"Assigned {amount:F2} to bucket {bucketId} for {month}");
}, budgetMonth, budgetBucketId, budgetAmount);
budgetCmd.AddCommand(budgetAssignCmd);

rootCommand.AddCommand(budgetCmd);

// ─── rates ────────────────────────────────────────────────────────────────────
var ratesCmd = new Command("rates", "Manage exchange rates");

var ratesUpdateCmd = new Command("update", "Trigger an exchange rate poll from upstream");
ratesUpdateCmd.SetHandler(async () =>
{
    var client = GetClient();
    await client.PollRatesAsync();
    Console.WriteLine("Exchange rates updated.");
});
ratesCmd.AddCommand(ratesUpdateCmd);

rootCommand.AddCommand(ratesCmd);

// ─── recurring ────────────────────────────────────────────────────────────────
var recurringCmd = new Command("recurring", "Manage recurring transactions");

var recurringListCmd = new Command("list", "List recurring transactions");
var recurringAccountId = new Option<Guid?>("--account-id", "Filter by account ID");
recurringListCmd.AddOption(recurringAccountId);
recurringListCmd.SetHandler(async (Guid? accountId) =>
{
    var client = GetClient();
    var items = await client.GetRecurringTransactionsAsync(accountId);
    if (items.Count == 0) { Console.WriteLine("No recurring transactions."); return; }
    Console.WriteLine($"{"ID",-36}  {"Payee",-30}  {"Amount",10}  {"Freq",-10}  {"Next",10}");
    Console.WriteLine(new string('-', 100));
    foreach (var r in items)
        Console.WriteLine($"{r.Id,-36}  {r.Payee,-30}  {r.AmountMilliunits / 1000m,10:F2}  {r.Frequency,-10}  {r.NextOccurrence?.ToString("yyyy-MM-dd") ?? "—",10}");
}, recurringAccountId);
recurringCmd.AddCommand(recurringListCmd);

var recurringRunCmd = new Command("run", "Process all due recurring transactions");
var recurringAsOf = new Option<string?>("--as-of", "Process as of date (YYYY-MM-DD), defaults to today");
recurringRunCmd.AddOption(recurringAsOf);
recurringRunCmd.SetHandler(async (string? asOf) =>
{
    var client = GetClient();
    DateOnly? asOfDate = asOf is not null ? DateOnly.ParseExact(asOf, "yyyy-MM-dd") : null;
    var result = await client.RunRecurringTransactionsAsync(asOfDate);
    Console.WriteLine($"Created {result.Created} transaction(s).");
    foreach (var id in result.TransactionIds)
        Console.WriteLine($"  {id}");
}, recurringAsOf);
recurringCmd.AddCommand(recurringRunCmd);

rootCommand.AddCommand(recurringCmd);

return await rootCommand.InvokeAsync(args);

// ─── helpers ──────────────────────────────────────────────────────────────────
static BucketBudgetClient GetClient()
{
    var config = CliConfig.Load();
    if (string.IsNullOrEmpty(config.Token))
    {
        Console.Error.WriteLine("Not authenticated. Run 'bb login --email EMAIL --password PASS' or set BUCKETBUDGET_TOKEN.");
        Environment.Exit(1);
    }
    return new BucketBudgetClient(config.BaseUrl, config.Token);
}

static List<CreateTransactionRequest> ParseCsv(string path, Guid accountId)
{
    var results = new List<CreateTransactionRequest>();
    var lines = File.ReadAllLines(path);
    if (lines.Length < 2) return results;

    // Expected header: Date,Payee,Amount,Memo,Cleared[,BucketId]
    foreach (var line in lines.Skip(1))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var cols = SplitCsvLine(line);
        if (cols.Count < 3) continue;
        var date = DateOnly.ParseExact(cols[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var payee = cols[1];
        var amount = decimal.Parse(cols[2], CultureInfo.InvariantCulture);
        var memo = cols.Count > 3 ? cols[3] : null;
        var cleared = cols.Count > 4 && bool.TryParse(cols[4], out var c) && c;
        Guid? bucketId = null;
        if (cols.Count > 5 && Guid.TryParse(cols[5], out var bid)) bucketId = bid;
        results.Add(new CreateTransactionRequest(accountId, bucketId, payee, (long)(amount * 1000), date, memo, cleared));
    }
    return results;
}

// RFC-4180 CSV field splitter: handles quoted fields and embedded commas.
static List<string> SplitCsvLine(string line)
{
    var fields = new List<string>();
    var sb = new System.Text.StringBuilder();
    bool inQuotes = false;

    for (int i = 0; i < line.Length; i++)
    {
        char c = line[i];
        if (inQuotes)
        {
            if (c == '"')
            {
                // Escaped quote "" inside a quoted field
                if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else inQuotes = false;
            }
            else sb.Append(c);
        }
        else
        {
            if (c == '"') inQuotes = true;
            else if (c == ',') { fields.Add(sb.ToString().Trim()); sb.Clear(); }
            else sb.Append(c);
        }
    }
    fields.Add(sb.ToString().Trim());
    return fields;
}
