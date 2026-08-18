using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.DataSourceAuthentications.Commands.DeleteDataSourceAuthentication;

internal sealed class DeleteDataSourceAuthenticationCommandHandler
    : IRequestHandler<DeleteDataSourceAuthenticationCommand>
{
    private readonly IAppDbContext _dbContext;

    public DeleteDataSourceAuthenticationCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        DeleteDataSourceAuthenticationCommand request,
        CancellationToken cancellationToken)
    {
        var authentication = await _dbContext.DataSourceAuthentications
            .FirstOrDefaultAsync(x => x.DataSourceId == request.DataSourceId, cancellationToken);

        if (authentication is null)
        {
            throw new KeyNotFoundException("DataSource authentication not found.");
        }

        _dbContext.DataSourceAuthentications.Remove(authentication);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
