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
    string? PasswordSourceKey) : IRequest;
