using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Abstractions.Headers;

public interface IExternalHeaderValueResolver
{
    Task<string?> ResolveAsync(
        HeaderValueSourceType valueSourceType,
        string sourceKey,
        CancellationToken cancellationToken);
}
