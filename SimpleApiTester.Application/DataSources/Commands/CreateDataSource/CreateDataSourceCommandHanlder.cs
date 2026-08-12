using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Operations.Persistence;
using SimpleApiTester.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

    namespace SimpleApiTester.Application.DataSources.Commands.CreateDataSource;

    internal sealed class CreateDataSourceCommandHandler
        : IRequestHandler<CreateDataSourceCommand, Guid>
    {
        private readonly IAppDbContext _dbContext;

        public CreateDataSourceCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> Handle(
            CreateDataSourceCommand request,
            CancellationToken cancellationToken)
        {
            var exists = await _dbContext.DataSources
                .AnyAsync(
                    x => x.Key == request.Key,
                    cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException(
                    $"DataSource with key '{request.Key}' already exists.");
            }

            var dataSource = new DataSource
            {
                Id = Guid.NewGuid(),
                Key = request.Key.Trim(),
                BaseUrl = request.BaseUrl.TrimEnd('/'),
                IsActive = true
            };

            _dbContext.DataSources.Add(dataSource);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return dataSource.Id;
        }
    }

