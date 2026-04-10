using System.Text.Json;

namespace BucketBudget.Cli;

public class CliConfig
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string? Token { get; set; }

    private static string ConfigPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "bucketbudget",
            "config.json");

    public static CliConfig Load()
    {
        var config = new CliConfig();

        // Load from file first
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                var stored = JsonSerializer.Deserialize<CliConfig>(json, JsonOptions);
                if (stored is not null)
                {
                    config.BaseUrl = stored.BaseUrl;
                    config.Token = stored.Token;
                }
            }
            catch { /* ignore corrupt config */ }
        }

        // Env vars override file
        var envUrl = Environment.GetEnvironmentVariable("BUCKETBUDGET_URL");
        if (!string.IsNullOrEmpty(envUrl))
            config.BaseUrl = envUrl;

        var envToken = Environment.GetEnvironmentVariable("BUCKETBUDGET_TOKEN");
        if (!string.IsNullOrEmpty(envToken))
            config.Token = envToken;

        return config;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
