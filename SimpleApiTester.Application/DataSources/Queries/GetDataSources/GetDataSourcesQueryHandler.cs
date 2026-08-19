

using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.DataSources.Queries.GetDataSources;

    internal sealed class GetDataSourcesQueryHandler
        : IRequestHandler<
            GetDataSourcesQuery,
            IReadOnlyList<DataSourceResponse>>
    {
        private readonly IAppDbContext _dbContext;

        public GetDataSourcesQueryHandler(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<DataSourceResponse>> Handle(
            GetDataSourcesQuery request,
            CancellationToken cancellationToken)
        {
            return await _dbContext.DataSources
                .AsNoTracking()
                .Select(x => new DataSourceResponse(
                    x.Id,
                    x.Key,
                    x.IsActive,
                    x.DefaultTimeoutSeconds))
                .ToListAsync(cancellationToken);
        }
    }

