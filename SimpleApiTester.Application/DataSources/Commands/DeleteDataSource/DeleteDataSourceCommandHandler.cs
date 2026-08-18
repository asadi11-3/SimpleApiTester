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

            if (dataSourceHeaders.Count != 0)
            {
                _dbContext.Headers.RemoveRange(dataSourceHeaders);
            }

            _dbContext.DataSources.Remove(dataSource);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
