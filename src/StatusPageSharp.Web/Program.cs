using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using StatusPageSharp.Domain.Constants;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.DependencyInjection;
using StatusPageSharp.Infrastructure.Identity;
using StatusPageSharp.Infrastructure.Setup;
using StatusPageSharp.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
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

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
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
