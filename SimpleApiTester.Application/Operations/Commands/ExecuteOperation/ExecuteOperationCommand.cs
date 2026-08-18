using MediatR;

namespace SimpleApiTester.Application.Operations.Commands.ExecuteOperation;

public sealed record ExecuteOperationCommand(Guid Id, Guid EnvironmentId)
    : IRequest<ExecuteOperationResponse>;
