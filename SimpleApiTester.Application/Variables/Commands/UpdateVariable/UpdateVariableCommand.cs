using MediatR;

namespace SimpleApiTester.Application.Variables.Commands.UpdateVariable;

public sealed record UpdateVariableCommand(
    Guid Id,
    string Key,
    string? Value,
    bool IsEnabled,
    bool IsSecret) : IRequest;
