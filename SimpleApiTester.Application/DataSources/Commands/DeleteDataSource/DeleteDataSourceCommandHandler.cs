using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources.Commands.DeleteDataSource
{
    internal sealed class DeleteDataSourceCommandHandler
        : IRequestHandler<DeleteDataSourceCommand>
    {
        private readonly IAppDbContext _dbContext;

        public DeleteDataSourceCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(
            DeleteDataSourceCommand request,
            CancellationToken cancellationToken)
        {
            var dataSource = await _dbContext.DataSources
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id,
                    cancellationToken);

            if (dataSource is null)
                throw new KeyNotFoundException("DataSource not found.");

            var dataSourceHeaders = await _dbContext.Headers
                .Where(x => x.DataSourceId == request.Id)
                .ToListAsync(cancellationToken);

            var authentication = await _dbContext.DataSourceAuthentications
                .FirstOrDefaultAsync(x => x.DataSourceId == request.Id, cancellationToken);

            var environments = await _dbContext.DataSourceEnvironments
                .Where(x => x.DataSourceId == request.Id)
                .ToListAsync(cancellationToken);

            var environmentIds = environments.Select(x => x.Id).ToList();

            var variables = environmentIds.Count == 0
                ? []
                : await _dbContext.Variables
                    .Where(x => environmentIds.Contains(x.DataSourceEnvironmentId))
                    .ToListAsync(cancellationToken);

            if (dataSourceHeaders.Count != 0)
            {
                _dbContext.Headers.RemoveRange(dataSourceHeaders);
            }

            if (authentication is not null)
            {
                _dbContext.DataSourceAuthentications.Remove(authentication);
            }

            if (environments.Count != 0)
            {
                _dbContext.DataSourceEnvironments.RemoveRange(environments);
            }

            if (variables.Count != 0)
            {
                _dbContext.Variables.RemoveRange(variables);
            }

            _dbContext.DataSources.Remove(dataSource);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
