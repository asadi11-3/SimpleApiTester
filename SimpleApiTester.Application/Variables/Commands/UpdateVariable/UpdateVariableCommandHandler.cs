using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Variables.Commands.UpdateVariable;

internal sealed class UpdateVariableCommandHandler : IRequestHandler<UpdateVariableCommand>
{
    private readonly IAppDbContext _dbContext;

    public UpdateVariableCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateVariableCommand request, CancellationToken cancellationToken)
    {
        var variable = await _dbContext.Variables
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (variable is null)
        {
            throw new KeyNotFoundException("Variable not found.");
        }

        var normalizedKey = request.Key.Trim();
        var normalizedKeyUpper = normalizedKey.ToUpperInvariant();

        var duplicateExists = await _dbContext.Variables
            .AnyAsync(
                x => x.Id != request.Id
                    && x.DataSourceId == variable.DataSourceId
                    && x.Key.ToUpper() == normalizedKeyUpper,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Variable with key '{normalizedKey}' already exists for this data source.");
        }

        variable.Key = normalizedKey;
        variable.Value = request.Value;
        variable.IsEnabled = request.IsEnabled;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
