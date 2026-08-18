using Microsoft.Extensions.Configuration;
using SimpleApiTester.Application.Abstractions.Headers;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Infrastructure.Services;

internal sealed class ExternalHeaderValueResolver : IExternalHeaderValueResolver
{
    private readonly IConfiguration _configuration;

    public ExternalHeaderValueResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<string?> ResolveAsync(
        HeaderValueSourceType valueSourceType,
        string sourceKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = valueSourceType switch
        {
            HeaderValueSourceType.UserSecret => _configuration[sourceKey],
            HeaderValueSourceType.EnvironmentVariable => Environment.GetEnvironmentVariable(sourceKey),
            _ => throw new InvalidOperationException($"Unsupported external header value source '{valueSourceType}'.")
        };

        return Task.FromResult(value);
    }
}
