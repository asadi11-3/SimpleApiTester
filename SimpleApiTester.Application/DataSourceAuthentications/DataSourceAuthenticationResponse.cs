using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.DataSourceAuthentications;

public sealed record DataSourceAuthenticationResponse(
    Guid Id,
    Guid DataSourceId,
    AuthenticationType AuthenticationType,
    HeaderValueSourceType ValueSourceType,
    string SourceKey,
    string? ApiKeyHeaderName);
