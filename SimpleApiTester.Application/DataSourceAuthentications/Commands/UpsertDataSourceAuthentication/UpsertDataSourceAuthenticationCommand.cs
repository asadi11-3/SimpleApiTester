using MediatR;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.DataSourceAuthentications.Commands.UpsertDataSourceAuthentication;

public sealed record UpsertDataSourceAuthenticationCommand(
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
    string? OAuthScope) : IRequest;
