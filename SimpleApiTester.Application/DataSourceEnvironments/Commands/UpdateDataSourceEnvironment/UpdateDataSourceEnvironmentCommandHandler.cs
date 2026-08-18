using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Application.DataSourceEnvironments;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.UpdateDataSourceEnvironment;

internal sealed class UpdateDataSourceEnvironmentCommandHandler
    : IRequestHandler<UpdateDataSourceEnvironmentCommand>
{
    private readonly IAppDbContext _dbContext;

    public UpdateDataSourceEnvironmentCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        UpdateDataSourceEnvironmentCommand request,
        CancellationToken cancellationToken)
    {
        var environment = await _dbContext.DataSourceEnvironments
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (environment is null)
        {
            throw new KeyNotFoundException("Environment not found.");
        }

        var normalizedName = request.Name.Trim();
        var normalizedNameUpper = normalizedName.ToUpperInvariant();
        var normalizedBaseUrl = DataSourceEnvironmentRules.NormalizeBaseUrl(request.BaseUrl);

        var duplicateExists = await _dbContext.DataSourceEnvironments
            .AnyAsync(
                x => x.Id != request.Id
                    && x.DataSourceId == environment.DataSourceId
                    && x.Name.ToUpper() == normalizedNameUpper,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Environment with name '{normalizedName}' already exists for this data source.");
        }

        environment.Name = normalizedName;
        environment.BaseUrl = normalizedBaseUrl;
        environment.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
