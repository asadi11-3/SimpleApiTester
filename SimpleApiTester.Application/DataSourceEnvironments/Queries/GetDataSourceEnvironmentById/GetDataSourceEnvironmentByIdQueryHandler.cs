using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Application.DataSourceEnvironments;

namespace SimpleApiTester.Application.DataSourceEnvironments.Queries.GetDataSourceEnvironmentById;

internal sealed class GetDataSourceEnvironmentByIdQueryHandler
    : IRequestHandler<GetDataSourceEnvironmentByIdQuery, DataSourceEnvironmentResponse>
{
    private readonly IAppDbContext _dbContext;

    public GetDataSourceEnvironmentByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DataSourceEnvironmentResponse> Handle(
        GetDataSourceEnvironmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var environment = await _dbContext.DataSourceEnvironments
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new DataSourceEnvironmentResponse(
                x.Id,
                x.DataSourceId,
                x.Name,
                x.BaseUrl,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (environment is null)
        {
            throw new KeyNotFoundException("Environment not found.");
        }

        return environment;
    }
}
