using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.DataSources.Queries.GetById
{
    internal sealed class GetDataSourceByIdQueryHandler
    : IRequestHandler<GetDataSourceByIdQuery, DataSourceResponse>
    {
        private readonly IAppDbContext _dbContext;

        public GetDataSourceByIdQueryHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DataSourceResponse> Handle(
            GetDataSourceByIdQuery request,
            CancellationToken cancellationToken)
        {
            var dataSource = await _dbContext.DataSources
                .AsNoTracking()
                .Where(x => x.Id == request.Id)
                .Select(x => new DataSourceResponse(
                    x.Id,
                    x.Key,
                    x.BaseUrl,
                    x.IsActive))
                .FirstOrDefaultAsync(cancellationToken);

            if (dataSource is null)
                throw new KeyNotFoundException("DataSource not found.");

            return dataSource;
        }
    }
}
