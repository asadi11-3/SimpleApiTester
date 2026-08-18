using MediatR;

namespace SimpleApiTester.Application.DataSources.Commands.CreateDataSource;

public sealed record CreateDataSourceCommand(
    string Key) : IRequest<Guid>;
