using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StatusPageSharp.Infrastructure.Data;

namespace StatusPageSharp.Infrastructure.Tests.Support;

public sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => new(options);

    public Task<ApplicationDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(CreateDbContext());
}
