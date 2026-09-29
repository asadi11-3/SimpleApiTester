using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SimpleApiTester.Infrastructure.Persistence;

namespace SimpleApiTester.Infrastructure.PostgreSqlMigrations;

public sealed class PostgreSqlAppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "ConnectionStrings__DefaultConnection";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"Environment variable '{ConnectionStringEnvironmentVariable}' is required.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(
                    typeof(PostgreSqlAppDbContextFactory).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
