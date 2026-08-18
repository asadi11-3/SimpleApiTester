using MediatR;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.UpdateDataSourceEnvironment;

public sealed record UpdateDataSourceEnvironmentCommand(
    Guid Id,
    string Name,
    string BaseUrl,
    bool IsActive) : IRequest;
