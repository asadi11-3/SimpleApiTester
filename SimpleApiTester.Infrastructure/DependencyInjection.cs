using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleApiTester.Application.Abstractions.Headers;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Infrastructure.Persistence;
using SimpleApiTester.Infrastructure.Services;
using System;

namespace SimpleApiTester.Infrastructure
{
    public static class DependencyInjection
    {
        private const string DefaultConnectionName = "DefaultConnection";

        private enum DatabaseProvider
        {
            SqlServer,
            PostgreSql
        }

        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpClient("OperationExecutor", client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            });

            services.AddHttpClient("ConnectionTester", client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            });

            services.AddHttpClient("OAuthTokenClient", client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            });

            services.AddScoped<SimpleApiTester.Application.Abstractions.Authentication.IOAuthTokenClient, OAuthTokenClient>();
            services.AddScoped<IOperationRequestExecutor, OperationRequestExecutor>();
            services.AddScoped<IDataSourceConnectionTester, DataSourceConnectionTester>();
            services.AddScoped<IExternalHeaderValueResolver, ExternalHeaderValueResolver>();

            var connectionString = configuration.GetConnectionString(DefaultConnectionName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{DefaultConnectionName}' is required.");
            }

            var providerName = configuration["Database:Provider"]
                ?? nameof(DatabaseProvider.SqlServer);

            if (!Enum.TryParse<DatabaseProvider>(providerName, true, out var provider))
            {
                throw new InvalidOperationException(
                    $"Unsupported database provider '{providerName}'. " +
                    $"Supported values are {nameof(DatabaseProvider.SqlServer)} and {nameof(DatabaseProvider.PostgreSql)}.");
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                switch (provider)
                {
                    case DatabaseProvider.SqlServer:
                        options.UseSqlServer(connectionString);
                        break;
                    case DatabaseProvider.PostgreSql:
                        options.UseNpgsql(
                            connectionString,
                            npgsql => npgsql.MigrationsAssembly(
                                "SimpleApiTester.Infrastructure.PostgreSqlMigrations"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
                }
            });

            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }
    }
}
