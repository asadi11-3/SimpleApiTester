using MediatR;

namespace SimpleApiTester.Application.Variables.Queries.GetVariablesByEnvironment;

public sealed record GetVariablesByEnvironmentQuery(Guid DataSourceEnvironmentId)
    : IRequest<IReadOnlyList<VariableResponse>>;
