using MediatR;

namespace SimpleApiTester.Application.DataSourceAuthentications.Commands.DeleteDataSourceAuthentication;

public sealed record DeleteDataSourceAuthenticationCommand(Guid DataSourceId) : IRequest;
