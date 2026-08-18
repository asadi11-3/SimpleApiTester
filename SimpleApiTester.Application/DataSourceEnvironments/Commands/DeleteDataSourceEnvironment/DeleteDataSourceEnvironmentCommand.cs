using MediatR;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.DeleteDataSourceEnvironment;

public sealed record DeleteDataSourceEnvironmentCommand(Guid Id) : IRequest;
