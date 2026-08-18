using MediatR;

namespace SimpleApiTester.Application.Headers.Queries.GetDataSourceHeaders;

public sealed record GetDataSourceHeadersQuery(Guid DataSourceId)
    : IRequest<IReadOnlyList<HeaderResponse>>;
