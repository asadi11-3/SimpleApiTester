using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.DataSourceAuthentications;

public sealed record DataSourceAuthenticationResponse(
    Guid Id,
    Guid DataSourceId,
    AuthenticationType AuthenticationType,
    HeaderValueSourceType? ValueSourceType,
    string? SourceKey,
    string? ApiKeyHeaderName,
    ApiKeyLocation? ApiKeyLocation,
    HeaderValueSourceType? UsernameSourceType,
    string? UsernameSourceKey,
    HeaderValueSourceType? PasswordSourceType,
    string? PasswordSourceKey,
    string? OAuthTokenEndpoint,
    HeaderValueSourceType? OAuthClientIdSourceType,
    string? OAuthClientIdSourceKey,
    HeaderValueSourceType? OAuthClientSecretSourceType,
    string? OAuthClientSecretSourceKey,
    string? OAuthScope);
