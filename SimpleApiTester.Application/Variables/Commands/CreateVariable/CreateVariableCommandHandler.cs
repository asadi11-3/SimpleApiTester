using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.Variables.Commands.CreateVariable;

internal sealed class CreateVariableCommandHandler : IRequestHandler<CreateVariableCommand, Guid>
{
    private readonly IAppDbContext _dbContext;

    public CreateVariableCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateVariableCommand request, CancellationToken cancellationToken)
    {
        var dataSourceExists = await _dbContext.DataSources
            .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

        if (!dataSourceExists)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        var normalizedKey = request.Key.Trim();
        var normalizedKeyUpper = normalizedKey.ToUpperInvariant();

        var duplicateExists = await _dbContext.Variables
            .AnyAsync(
                x => x.DataSourceId == request.DataSourceId && x.Key.ToUpper() == normalizedKeyUpper,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Variable with key '{normalizedKey}' already exists for this data source.");
        }

        var variable = new Variable
        {
            Id = Guid.NewGuid(),
            DataSourceId = request.DataSourceId,
            Key = normalizedKey,
            Value = request.Value,
            IsEnabled = request.IsEnabled
        };

        _dbContext.Variables.Add(variable);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return variable.Id;
    }
}
