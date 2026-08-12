using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Infrastructure.Persistence;
using SimpleApiTester.Infrastructure.Services;

namespace SimpleApiTester.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpClient("OperationExecutor");

            services.AddScoped<IOperationRequestExecutor, OperationRequestExecutor>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            return services;
        }
    }
}
