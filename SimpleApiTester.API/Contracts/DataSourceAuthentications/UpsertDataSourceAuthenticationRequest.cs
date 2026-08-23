using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.API.Contracts.DataSourceAuthentications;

public sealed record UpsertDataSourceAuthenticationRequest(
    AuthenticationType AuthenticationType,
    HeaderValueSourceType? ValueSourceType,
    string? SourceKey,
    string? ApiKeyHeaderName,
    HeaderValueSourceType? UsernameSourceType,
    string? UsernameSourceKey,
    HeaderValueSourceType? PasswordSourceType,
    string? PasswordSourceKey);
