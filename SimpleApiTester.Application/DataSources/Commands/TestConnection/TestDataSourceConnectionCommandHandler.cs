using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.DataSources.Commands.TestConnection;

internal sealed class TestDataSourceConnectionCommandHandler
    : IRequestHandler<TestDataSourceConnectionCommand, TestDataSourceConnectionResponse>
{
    private readonly IAppDbContext _dbContext;
    private readonly IDataSourceConnectionTester _dataSourceConnectionTester;

    public TestDataSourceConnectionCommandHandler(
        IAppDbContext dbContext,
        IDataSourceConnectionTester dataSourceConnectionTester)
    {
        _dbContext = dbContext;
        _dataSourceConnectionTester = dataSourceConnectionTester;
    }

    public async Task<TestDataSourceConnectionResponse> Handle(
        TestDataSourceConnectionCommand request,
        CancellationToken cancellationToken)
    {
        var dataSource = await _dbContext.DataSources
            .AsNoTracking()
            .Where(x => x.Id == request.DataSourceId)
            .Select(x => new
            {
                x.IsActive,
                x.DefaultTimeoutSeconds
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dataSource is null)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        var environment = await _dbContext.DataSourceEnvironments
            .AsNoTracking()
            .Where(x => x.Id == request.EnvironmentId && x.DataSourceId == request.DataSourceId)
            .Select(x => new
            {
                x.BaseUrl,
                x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (environment is null)
        {
            throw new KeyNotFoundException("Environment not found.");
        }

        if (!dataSource.IsActive)
        {
            throw new InvalidOperationException("Cannot execute an operation for an inactive data source.");
        }

        if (!environment.IsActive)
        {
            throw new InvalidOperationException("Cannot execute an operation for an inactive environment.");
        }

        return await _dataSourceConnectionTester.TestConnectionAsync(
            environment.BaseUrl,
            DataSourceTimeoutPolicy.Resolve(dataSource.DefaultTimeoutSeconds),
            cancellationToken);
    }
}
