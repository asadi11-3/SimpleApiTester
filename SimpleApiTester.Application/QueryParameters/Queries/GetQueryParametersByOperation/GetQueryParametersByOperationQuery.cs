using MediatR;

namespace SimpleApiTester.Application.QueryParameters.Queries.GetQueryParametersByOperation;

public sealed record GetQueryParametersByOperationQuery(Guid OperationId)
    : IRequest<IReadOnlyList<QueryParameterResponse>>;
