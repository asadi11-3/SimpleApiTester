using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.DataSourceAuthentications.Queries.GetDataSourceAuthentication;

internal sealed class GetDataSourceAuthenticationQueryHandler
    : IRequestHandler<GetDataSourceAuthenticationQuery, DataSourceAuthenticationResponse>
{
    private readonly IAppDbContext _dbContext;

    public GetDataSourceAuthenticationQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DataSourceAuthenticationResponse> Handle(
        GetDataSourceAuthenticationQuery request,
        CancellationToken cancellationToken)
    {
        var authentication = await _dbContext.DataSourceAuthentications
            .AsNoTracking()
            .Where(x => x.DataSourceId == request.DataSourceId)
            .Select(x => new DataSourceAuthenticationResponse(
                x.Id,
                x.DataSourceId,
                x.AuthenticationType,
                x.ValueSourceType,
                x.SourceKey,
                x.ApiKeyHeaderName,
                x.ApiKeyLocation,
                x.UsernameSourceType,
                x.UsernameSourceKey,
                x.PasswordSourceType,
                x.PasswordSourceKey))
            .FirstOrDefaultAsync(cancellationToken);

        if (authentication is null)
        {
            throw new KeyNotFoundException("DataSource authentication not found.");
        }

        return authentication;
    }
}
