using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources.Commands.UpdateDataSource
{
    internal sealed class UpdateDataSourceCommandHandler
     : IRequestHandler<UpdateDataSourceCommand>
    {
        private readonly IAppDbContext _dbContext;

        public UpdateDataSourceCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Handle(
            UpdateDataSourceCommand request,
            CancellationToken cancellationToken)
        {
            var dataSource = await _dbContext.DataSources
                .FirstOrDefaultAsync(
                    x => x.Id == request.Id,
                    cancellationToken);

            if (dataSource is null)
                throw new KeyNotFoundException("DataSource not found.");

            var normalizedKey = request.Key.Trim();

            var duplicate = await _dbContext.DataSources
                .AnyAsync(
                    x => x.Id != request.Id &&
                         x.Key == normalizedKey,
                    cancellationToken);

            if (duplicate)
                throw new InvalidOperationException(
                    $"DataSource with key '{normalizedKey}' already exists.");

            dataSource.Key = normalizedKey;
            dataSource.IsActive = request.IsActive;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
