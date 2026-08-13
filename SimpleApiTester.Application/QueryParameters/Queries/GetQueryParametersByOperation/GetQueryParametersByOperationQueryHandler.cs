using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.QueryParameters.Queries.GetQueryParametersByOperation;

internal sealed class GetQueryParametersByOperationQueryHandler
    : IRequestHandler<GetQueryParametersByOperationQuery, IReadOnlyList<QueryParameterResponse>>
{
    private readonly IAppDbContext _dbContext;

    public GetQueryParametersByOperationQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<QueryParameterResponse>> Handle(
        GetQueryParametersByOperationQuery request,
        CancellationToken cancellationToken)
    {
        var operationExists = await _dbContext.Operations
            .AnyAsync(x => x.Id == request.OperationId, cancellationToken);

        if (!operationExists)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        return await _dbContext.QueryParameters
            .AsNoTracking()
            .Where(x => x.OperationId == request.OperationId)
            .Select(x => new QueryParameterResponse(
                x.Id,
                x.OperationId,
                x.Key,
                x.Value,
                x.IsEnabled))
            .ToListAsync(cancellationToken);
    }
}
