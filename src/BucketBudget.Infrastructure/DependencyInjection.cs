using BucketBudget.Application.Interfaces;
using BucketBudget.Infrastructure.ExchangeRates;
using BucketBudget.Infrastructure.Persistence;
using BucketBudget.Infrastructure.RecurringTransactions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BucketBudget.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BudgetDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BudgetDb")));

        services.AddScoped<IBudgetDbContext>(provider => provider.GetRequiredService<BudgetDbContext>());

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<BudgetDbContext>();

        // Exchange rate polling
        services.AddHttpClient<DolarApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://dolarapi.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<ExchangeRatePollingService>();
        services.AddHostedService(sp => sp.GetRequiredService<ExchangeRatePollingService>());
        services.AddSingleton<IExchangeRatePoller>(sp => sp.GetRequiredService<ExchangeRatePollingService>());

        // Recurring transaction auto-posting
        services.AddHostedService<RecurringTransactionPostingService>();

        return services;
    }
}
