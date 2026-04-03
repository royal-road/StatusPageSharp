using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using StatusPageSharp.Domain.Constants;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.DependencyInjection;
using StatusPageSharp.Infrastructure.Identity;
using StatusPageSharp.Infrastructure.Setup;
using StatusPageSharp.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddStatusPageForwardedHeaders(builder.Configuration);
builder.Services.AddStatusPageBootstrapOptions(builder.Configuration);
builder.Services.AddStatusPageInfrastructureForWeb(builder.Configuration);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder
    .Services.AddDefaultIdentity<ApplicationUser>(StatusPageIdentityOptionsConfiguration.Configure)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleNames.Administrator));
});
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/identity/account/login";
    options.AccessDeniedPath = "/identity/account/accessdenied";
    options.Events.OnValidatePrincipal = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<
            UserManager<ApplicationUser>
        >();
        var user = await userManager.GetUserAsync(context.Principal!);
        if (user is { IsEnabled: false })
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("StatusCard", policy => policy.Expire(TimeSpan.FromSeconds(30)));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});

var app = builder.Build();
var runningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase
);

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

await app.Services.InitializeStatusPageAsync();

if (!runningInContainer)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapStatusPageApis();

app.Run();
