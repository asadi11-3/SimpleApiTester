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
        var normalizedApiKeyLocation = request.AuthenticationType == AuthenticationType.ApiKey
            ? DataSourceAuthenticationRules.NormalizeApiKeyLocation(request.ApiKeyLocation)
            : (ApiKeyLocation?)null;
        var normalizedUsernameSourceKey = DataSourceAuthenticationRules.NormalizeSourceKey(request.UsernameSourceKey);
        var normalizedPasswordSourceKey = DataSourceAuthenticationRules.NormalizeSourceKey(request.PasswordSourceKey);
        var normalizedOAuthTokenEndpoint = DataSourceAuthenticationRules.NormalizeOAuthTokenEndpoint(request.OAuthTokenEndpoint);
        var normalizedOAuthClientIdSourceKey = DataSourceAuthenticationRules.NormalizeSourceKey(request.OAuthClientIdSourceKey);
        var normalizedOAuthClientSecretSourceKey = DataSourceAuthenticationRules.NormalizeSourceKey(request.OAuthClientSecretSourceKey);
        var normalizedOAuthScope = DataSourceAuthenticationRules.NormalizeOAuthScope(request.OAuthScope);

        if (request.AuthenticationType == AuthenticationType.ApiKey
            && normalizedApiKeyLocation == ApiKeyLocation.Query)
        {
            await DataSourceAuthenticationConflictGuard.EnsureNoEnabledQueryParameterConflictAsync(
                _dbContext,
                request.DataSourceId,
                request.AuthenticationType,
                normalizedApiKeyLocation,
                normalizedApiKeyHeaderName,
                cancellationToken);
        }
        else if (request.AuthenticationType == AuthenticationType.OAuthClientCredentials)
        {
            await DataSourceAuthenticationConflictGuard.EnsureNoEnabledHeaderConflictAsync(
                _dbContext,
                request.DataSourceId,
                request.AuthenticationType,
                null,
                null,
                cancellationToken);
        }
        else
        {
            await DataSourceAuthenticationConflictGuard.EnsureNoEnabledHeaderConflictAsync(
                _dbContext,
                request.DataSourceId,
                request.AuthenticationType,
                normalizedApiKeyLocation,
                normalizedApiKeyHeaderName,
                cancellationToken);
        }

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
            authentication.ApiKeyLocation = null;
            authentication.UsernameSourceType = request.UsernameSourceType;
            authentication.UsernameSourceKey = normalizedUsernameSourceKey;
            authentication.PasswordSourceType = request.PasswordSourceType;
            authentication.PasswordSourceKey = normalizedPasswordSourceKey;
            authentication.OAuthTokenEndpoint = null;
            authentication.OAuthClientIdSourceType = null;
            authentication.OAuthClientIdSourceKey = null;
            authentication.OAuthClientSecretSourceType = null;
            authentication.OAuthClientSecretSourceKey = null;
            authentication.OAuthScope = null;
        }
        else if (request.AuthenticationType == AuthenticationType.OAuthClientCredentials)
        {
            authentication.ValueSourceType = null;
            authentication.SourceKey = null;
            authentication.ApiKeyHeaderName = null;
            authentication.ApiKeyLocation = null;
            authentication.UsernameSourceType = null;
            authentication.UsernameSourceKey = null;
            authentication.PasswordSourceType = null;
            authentication.PasswordSourceKey = null;
            authentication.OAuthTokenEndpoint = normalizedOAuthTokenEndpoint;
            authentication.OAuthClientIdSourceType = request.OAuthClientIdSourceType;
            authentication.OAuthClientIdSourceKey = normalizedOAuthClientIdSourceKey;
            authentication.OAuthClientSecretSourceType = request.OAuthClientSecretSourceType;
            authentication.OAuthClientSecretSourceKey = normalizedOAuthClientSecretSourceKey;
            authentication.OAuthScope = normalizedOAuthScope;
        }
        else
        {
            authentication.ValueSourceType = request.ValueSourceType;
            authentication.SourceKey = normalizedSourceKey;
            authentication.ApiKeyHeaderName = normalizedApiKeyHeaderName;
            authentication.ApiKeyLocation = request.AuthenticationType == AuthenticationType.ApiKey
                ? normalizedApiKeyLocation
                : null;
            authentication.UsernameSourceType = null;
            authentication.UsernameSourceKey = null;
            authentication.PasswordSourceType = null;
            authentication.PasswordSourceKey = null;
            authentication.OAuthTokenEndpoint = null;
            authentication.OAuthClientIdSourceType = null;
            authentication.OAuthClientIdSourceKey = null;
            authentication.OAuthClientSecretSourceType = null;
            authentication.OAuthClientSecretSourceKey = null;
            authentication.OAuthScope = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
