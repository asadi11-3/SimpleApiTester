using MediatR;

namespace SimpleApiTester.Application.QueryParameters.Commands.UpdateQueryParameter;

public sealed record UpdateQueryParameterCommand(
    Guid Id,
    Guid OperationId,
    string Key,
    string? Value,
    bool IsEnabled) : IRequest;
