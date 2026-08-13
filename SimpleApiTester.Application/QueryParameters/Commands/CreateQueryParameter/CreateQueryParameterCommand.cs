using MediatR;

namespace SimpleApiTester.Application.QueryParameters.Commands.CreateQueryParameter;

public sealed record CreateQueryParameterCommand(
    Guid OperationId,
    string Key,
    string? Value,
    bool IsEnabled) : IRequest<Guid>;
