using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.API.Contracts.DataSourceAuthentications;

public sealed record UpsertDataSourceAuthenticationRequest(
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
