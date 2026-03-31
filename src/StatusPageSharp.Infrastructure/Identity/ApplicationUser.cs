using Microsoft.AspNetCore.Identity;

namespace StatusPageSharp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
}
