using Microsoft.AspNetCore.Identity;

namespace BucketBudget.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
