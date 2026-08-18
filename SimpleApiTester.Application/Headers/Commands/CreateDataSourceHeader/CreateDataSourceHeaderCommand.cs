using MediatR;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Headers.Commands.CreateDataSourceHeader;

public sealed record CreateDataSourceHeaderCommand(
    Guid DataSourceId,
    string Key,
    HeaderValueSourceType ValueSourceType,
    string? Value,
    string? SourceKey,
    bool IsEnabled) : IRequest<Guid>;
