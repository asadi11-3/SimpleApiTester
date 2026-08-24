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

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }
    }
}
