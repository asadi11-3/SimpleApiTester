using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Application.DataSourceEnvironments;

namespace SimpleApiTester.Application.DataSourceEnvironments.Queries.GetDataSourceEnvironments;

internal sealed class GetDataSourceEnvironmentsQueryHandler
    : IRequestHandler<GetDataSourceEnvironmentsQuery, IReadOnlyList<DataSourceEnvironmentResponse>>
{
    private readonly IAppDbContext _dbContext;

    public GetDataSourceEnvironmentsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DataSourceEnvironmentResponse>> Handle(
        GetDataSourceEnvironmentsQuery request,
        CancellationToken cancellationToken)
    {
        var dataSourceExists = await _dbContext.DataSources
            .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

        if (!dataSourceExists)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        return await _dbContext.DataSourceEnvironments
            .AsNoTracking()
            .Where(x => x.DataSourceId == request.DataSourceId)
            .OrderBy(x => x.Name)
            .Select(x => new DataSourceEnvironmentResponse(
                x.Id,
                x.DataSourceId,
                x.Name,
                x.BaseUrl,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }
}
