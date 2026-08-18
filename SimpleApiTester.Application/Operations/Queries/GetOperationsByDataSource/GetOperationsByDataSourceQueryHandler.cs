using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.Operations.Queries.GetOperationsByDataSource
{
    internal sealed class GetOperationsByDataSourceQueryHandler
    : IRequestHandler<
        GetOperationsByDataSourceQuery,
        IReadOnlyList<OperationResponse>>
    {
        private readonly IAppDbContext _dbContext;

        public GetOperationsByDataSourceQueryHandler(
            IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<OperationResponse>> Handle(
            GetOperationsByDataSourceQuery request,
            CancellationToken cancellationToken)
        {
            var dataSourceExists = await _dbContext.DataSources
                .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

            if (!dataSourceExists)
            {
                throw new KeyNotFoundException("DataSource not found.");
            }

            return await _dbContext.Operations
                .AsNoTracking()
                .Where(x => x.DataSourceId == request.DataSourceId)
                .Select(x => new OperationResponse(
                    x.Id,
                    x.DataSourceId,
                    x.ApiName,
                    x.Endpoint,
                    x.MethodType,
                    x.Body,
                    x.ContentType,
                    x.AuthenticationMode))
                .ToListAsync(cancellationToken);
        }
    }
}
