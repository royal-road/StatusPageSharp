using Microsoft.AspNetCore.Identity;

namespace StatusPageSharp.Infrastructure.Identity;

public static class StatusPageIdentityOptionsConfiguration
{
    public static void Configure(IdentityOptions options)
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredLength = 8;
        options.Stores.MaxLengthForKeys = 128;
    }
}
