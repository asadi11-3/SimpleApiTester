using MediatR;
using SimpleApiTester.Application.DataSourceEnvironments;

namespace SimpleApiTester.Application.DataSourceEnvironments.Queries.GetDataSourceEnvironmentById;

public sealed record GetDataSourceEnvironmentByIdQuery(Guid Id) : IRequest<DataSourceEnvironmentResponse>;
