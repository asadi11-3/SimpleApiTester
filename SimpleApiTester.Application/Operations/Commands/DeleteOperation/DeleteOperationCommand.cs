using MediatR;

namespace SimpleApiTester.Application.Operations.Commands.DeleteOperation;

public sealed record DeleteOperationCommand(Guid Id) : IRequest;
