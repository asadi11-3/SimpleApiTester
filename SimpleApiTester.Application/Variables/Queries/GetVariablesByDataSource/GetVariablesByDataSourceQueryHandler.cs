using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Variables.Queries.GetVariablesByDataSource;

internal sealed class GetVariablesByDataSourceQueryHandler
    : IRequestHandler<GetVariablesByDataSourceQuery, IReadOnlyList<VariableResponse>>
{
    private readonly IAppDbContext _dbContext;

    public GetVariablesByDataSourceQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<VariableResponse>> Handle(
        GetVariablesByDataSourceQuery request,
        CancellationToken cancellationToken)
    {
        var dataSourceExists = await _dbContext.DataSources
            .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

        if (!dataSourceExists)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        return await _dbContext.Variables
            .AsNoTracking()
            .Where(x => x.DataSourceId == request.DataSourceId)
            .Select(x => new VariableResponse(
                x.Id,
                x.DataSourceId,
                x.Key,
                x.Value,
                x.IsEnabled))
            .ToListAsync(cancellationToken);
    }
}
