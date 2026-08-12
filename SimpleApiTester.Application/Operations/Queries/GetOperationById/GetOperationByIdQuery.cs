using MediatR;

namespace SimpleApiTester.Application.Operations.Queries.GetOperationById;

public sealed record GetOperationByIdQuery(Guid Id)
    : IRequest<OperationResponse>;
