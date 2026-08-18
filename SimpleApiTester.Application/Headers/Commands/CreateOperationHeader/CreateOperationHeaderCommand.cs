using MediatR;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Headers.Commands.CreateOperationHeader;

public sealed record CreateOperationHeaderCommand(
    Guid OperationId,
    string Key,
    HeaderValueSourceType ValueSourceType,
    string? Value,
    string? SourceKey,
    bool IsEnabled) : IRequest<Guid>;
