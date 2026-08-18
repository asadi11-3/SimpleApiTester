using MediatR;
using SimpleApiTester.Application.DataSourceEnvironments;

namespace SimpleApiTester.Application.DataSourceEnvironments.Queries.GetDataSourceEnvironments;

public sealed record GetDataSourceEnvironmentsQuery(Guid DataSourceId)
    : IRequest<IReadOnlyList<DataSourceEnvironmentResponse>>;
