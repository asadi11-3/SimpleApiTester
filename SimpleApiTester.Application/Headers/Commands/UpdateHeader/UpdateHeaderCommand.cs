using MediatR;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Headers.Commands.UpdateHeader;

public sealed record UpdateHeaderCommand(
    Guid Id,
    string Key,
    HeaderValueSourceType ValueSourceType,
    string? Value,
    string? SourceKey,
    bool IsEnabled) : IRequest;
