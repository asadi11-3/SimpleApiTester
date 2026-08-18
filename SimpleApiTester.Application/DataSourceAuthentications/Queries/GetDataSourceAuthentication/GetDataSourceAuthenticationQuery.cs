using MediatR;

namespace SimpleApiTester.Application.DataSourceAuthentications.Queries.GetDataSourceAuthentication;

public sealed record GetDataSourceAuthenticationQuery(Guid DataSourceId)
    : IRequest<DataSourceAuthenticationResponse>;
