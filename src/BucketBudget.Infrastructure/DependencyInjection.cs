using BucketBudget.Application.Interfaces;
using BucketBudget.Infrastructure.Persistence;
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

        return services;
    }
}
