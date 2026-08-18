using MediatR;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.CreateDataSourceEnvironment;

public sealed record CreateDataSourceEnvironmentCommand(
    Guid DataSourceId,
    string Name,
    string BaseUrl,
    bool IsActive) : IRequest<Guid>;
