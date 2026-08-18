using MediatR;

namespace SimpleApiTester.Application.Headers.Queries.GetOperationHeaders;

public sealed record GetOperationHeadersQuery(Guid OperationId)
    : IRequest<IReadOnlyList<HeaderResponse>>;
