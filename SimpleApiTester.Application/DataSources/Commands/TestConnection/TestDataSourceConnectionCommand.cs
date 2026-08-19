using MediatR;

namespace SimpleApiTester.Application.DataSources.Commands.TestConnection;

public sealed record TestDataSourceConnectionCommand(
    Guid DataSourceId,
    Guid EnvironmentId)
    : IRequest<TestDataSourceConnectionResponse>;
