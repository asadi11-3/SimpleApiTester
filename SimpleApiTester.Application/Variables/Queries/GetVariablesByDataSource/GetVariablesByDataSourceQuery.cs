using MediatR;

namespace SimpleApiTester.Application.Variables.Queries.GetVariablesByDataSource;

public sealed record GetVariablesByDataSourceQuery(Guid DataSourceId)
    : IRequest<IReadOnlyList<VariableResponse>>;
