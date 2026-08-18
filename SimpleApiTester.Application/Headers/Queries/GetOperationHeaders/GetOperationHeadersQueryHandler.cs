using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Headers.Queries.GetOperationHeaders;

internal sealed class GetOperationHeadersQueryHandler
    : IRequestHandler<GetOperationHeadersQuery, IReadOnlyList<HeaderResponse>>
{
    private readonly IAppDbContext _dbContext;

    public GetOperationHeadersQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<HeaderResponse>> Handle(
        GetOperationHeadersQuery request,
        CancellationToken cancellationToken)
    {
        var operationExists = await _dbContext.Operations
            .AnyAsync(x => x.Id == request.OperationId, cancellationToken);

        if (!operationExists)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        return await _dbContext.Headers
            .AsNoTracking()
            .Where(x => x.OperationId == request.OperationId)
            .Select(x => new HeaderResponse(
                x.Id,
                x.DataSourceId,
                x.OperationId,
                x.Key,
                x.Value,
                x.ValueSourceType,
                x.SourceKey,
                x.IsEnabled))
            .ToListAsync(cancellationToken);
    }
}
