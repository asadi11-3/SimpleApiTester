using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Headers.Queries.GetDataSourceHeaders;

internal sealed class GetDataSourceHeadersQueryHandler
    : IRequestHandler<GetDataSourceHeadersQuery, IReadOnlyList<HeaderResponse>>
{
    private readonly IAppDbContext _dbContext;

    public GetDataSourceHeadersQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<HeaderResponse>> Handle(
        GetDataSourceHeadersQuery request,
        CancellationToken cancellationToken)
    {
        var dataSourceExists = await _dbContext.DataSources
            .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

        if (!dataSourceExists)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        return await _dbContext.Headers
            .AsNoTracking()
            .Where(x => x.DataSourceId == request.DataSourceId)
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
