using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.DataSourceEnvironments.Commands.DeleteDataSourceEnvironment;

internal sealed class DeleteDataSourceEnvironmentCommandHandler : IRequestHandler<DeleteDataSourceEnvironmentCommand>
{
    private readonly IAppDbContext _dbContext;

    public DeleteDataSourceEnvironmentCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteDataSourceEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var environment = await _dbContext.DataSourceEnvironments
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (environment is null)
        {
            throw new KeyNotFoundException("Environment not found.");
        }

        var variables = await _dbContext.Variables
            .Where(x => x.DataSourceEnvironmentId == request.Id)
            .ToListAsync(cancellationToken);

        if (variables.Count != 0)
        {
            _dbContext.Variables.RemoveRange(variables);
        }

        _dbContext.DataSourceEnvironments.Remove(environment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
