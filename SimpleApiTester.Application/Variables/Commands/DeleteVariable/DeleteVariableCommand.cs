using MediatR;

namespace SimpleApiTester.Application.Variables.Commands.DeleteVariable;

public sealed record DeleteVariableCommand(Guid Id) : IRequest;
