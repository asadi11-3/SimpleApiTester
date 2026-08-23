using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Domain.Entities;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.DataSourceAuthentications.Commands.UpsertDataSourceAuthentication;

internal sealed class UpsertDataSourceAuthenticationCommandHandler
    : IRequestHandler<UpsertDataSourceAuthenticationCommand>
{
    private readonly IAppDbContext _dbContext;

    public UpsertDataSourceAuthenticationCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        UpsertDataSourceAuthenticationCommand request,
        CancellationToken cancellationToken)
    {
        var dataSourceExists = await _dbContext.DataSources
            .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

        if (!dataSourceExists)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        var normalizedSourceKey = DataSourceAuthenticationRules.NormalizeSourceKey(request.SourceKey);
        var normalizedApiKeyHeaderName = DataSourceAuthenticationRules.NormalizeApiKeyHeaderName(request.ApiKeyHeaderName);
        var normalizedUsernameSourceKey = DataSourceAuthenticationRules.NormalizeSourceKey(request.UsernameSourceKey);
        var normalizedPasswordSourceKey = DataSourceAuthenticationRules.NormalizeSourceKey(request.PasswordSourceKey);

        await DataSourceAuthenticationConflictGuard.EnsureNoEnabledHeaderConflictAsync(
            _dbContext,
            request.DataSourceId,
            request.AuthenticationType,
            normalizedApiKeyHeaderName,
            cancellationToken);

        var authentication = await _dbContext.DataSourceAuthentications
            .FirstOrDefaultAsync(x => x.DataSourceId == request.DataSourceId, cancellationToken);

        if (authentication is null)
        {
            authentication = new DataSourceAuthentication
            {
                Id = Guid.NewGuid(),
                DataSourceId = request.DataSourceId
            };

            _dbContext.DataSourceAuthentications.Add(authentication);
        }

        authentication.AuthenticationType = request.AuthenticationType;

        if (request.AuthenticationType == AuthenticationType.Basic)
        {
            authentication.ValueSourceType = null;
            authentication.SourceKey = null;
            authentication.ApiKeyHeaderName = null;
            authentication.UsernameSourceType = request.UsernameSourceType;
            authentication.UsernameSourceKey = normalizedUsernameSourceKey;
            authentication.PasswordSourceType = request.PasswordSourceType;
            authentication.PasswordSourceKey = normalizedPasswordSourceKey;
        }
        else
        {
            authentication.ValueSourceType = request.ValueSourceType;
            authentication.SourceKey = normalizedSourceKey;
            authentication.ApiKeyHeaderName = normalizedApiKeyHeaderName;
            authentication.UsernameSourceType = null;
            authentication.UsernameSourceKey = null;
            authentication.PasswordSourceType = null;
            authentication.PasswordSourceKey = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
