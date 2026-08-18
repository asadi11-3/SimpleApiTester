using MediatR;

namespace SimpleApiTester.Application.Variables.Commands.CreateVariable;

public sealed record CreateVariableCommand(
    Guid DataSourceId,
    string Key,
    string? Value,
    bool IsEnabled) : IRequest<Guid>;
