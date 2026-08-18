using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.Headers.Commands.CreateDataSourceHeader;

internal sealed class CreateDataSourceHeaderCommandHandler : IRequestHandler<CreateDataSourceHeaderCommand, Guid>
{
    private readonly IAppDbContext _dbContext;

    public CreateDataSourceHeaderCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateDataSourceHeaderCommand request, CancellationToken cancellationToken)
    {
        var dataSourceExists = await _dbContext.DataSources
            .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

        if (!dataSourceExists)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        var header = await HeaderFactory.CreateDataSourceHeaderAsync(_dbContext, request, cancellationToken);

        _dbContext.Headers.Add(header);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return header.Id;
    }
}
