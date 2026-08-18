using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Application.DataSourceEnvironments;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.CreateDataSourceEnvironment;

internal sealed class CreateDataSourceEnvironmentCommandHandler
    : IRequestHandler<CreateDataSourceEnvironmentCommand, Guid>
{
    private readonly IAppDbContext _dbContext;

    public CreateDataSourceEnvironmentCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        CreateDataSourceEnvironmentCommand request,
        CancellationToken cancellationToken)
    {
        var dataSourceExists = await _dbContext.DataSources
            .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

        if (!dataSourceExists)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        var normalizedName = request.Name.Trim();
        var normalizedNameUpper = normalizedName.ToUpperInvariant();
        var normalizedBaseUrl = DataSourceEnvironmentRules.NormalizeBaseUrl(request.BaseUrl);

        var duplicateExists = await _dbContext.DataSourceEnvironments
            .AnyAsync(
                x => x.DataSourceId == request.DataSourceId && x.Name.ToUpper() == normalizedNameUpper,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Environment with name '{normalizedName}' already exists for this data source.");
        }

        var environment = new DataSourceEnvironment
        {
            Id = Guid.NewGuid(),
            DataSourceId = request.DataSourceId,
            Name = normalizedName,
            BaseUrl = normalizedBaseUrl,
            IsActive = request.IsActive
        };

        _dbContext.DataSourceEnvironments.Add(environment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return environment.Id;
    }
}
