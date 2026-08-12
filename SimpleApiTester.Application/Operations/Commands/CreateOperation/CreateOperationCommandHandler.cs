using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.Operations.Commands.CreateOperation
{
    internal sealed class CreateOperationCommandHandler
     : IRequestHandler<CreateOperationCommand, Guid>
    {
        private readonly IAppDbContext _dbContext;

        public CreateOperationCommandHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> Handle(
            CreateOperationCommand request,
            CancellationToken cancellationToken)
        {
            var dataSourceExists = await _dbContext.DataSources
                .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

            if (!dataSourceExists)
            {
                throw new KeyNotFoundException("DataSource not found.");
            }

            var normalizedApiName = request.ApiName.Trim();
            var normalizedEndpoint = NormalizeEndpoint(request.Endpoint);

            var operation = new Operation
            {
                Id = Guid.NewGuid(),
                DataSourceId = request.DataSourceId,
                ApiName = normalizedApiName,
                Endpoint = normalizedEndpoint,
                MethodType = request.MethodType,
                Body = request.Body
            };

            _dbContext.Operations.Add(operation);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return operation.Id;
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            var trimmedEndpoint = endpoint.Trim();

            return "/" + trimmedEndpoint.TrimStart('/');
        }
    }
}
