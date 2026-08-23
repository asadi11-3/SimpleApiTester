using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.DataSourceAuthentications;

internal static class DataSourceAuthenticationConflictGuard
{
    public static async Task EnsureNoEnabledHeaderConflictAsync(
        IAppDbContext dbContext,
        Guid dataSourceId,
        AuthenticationType authenticationType,
        ApiKeyLocation? apiKeyLocation,
        string? apiKeyHeaderName,
        CancellationToken cancellationToken,
        Guid? ignoredHeaderId = null)
    {
        var effectiveHeaderName = DataSourceAuthenticationRules
            .GetEffectiveHeaderName(authenticationType, apiKeyLocation, apiKeyHeaderName);

        if (effectiveHeaderName is null)
        {
            return;
        }

        var effectiveHeaderNameUpper = effectiveHeaderName.ToUpperInvariant();

        var conflictingDataSourceHeaderExists = await dbContext.Headers.AnyAsync(
            x => x.Id != ignoredHeaderId
                && x.IsEnabled
                && x.DataSourceId == dataSourceId
                && x.Key.ToUpper() == effectiveHeaderNameUpper,
            cancellationToken);

        if (conflictingDataSourceHeaderExists)
        {
            throw new InvalidOperationException(
                DataSourceAuthenticationRules.CreateConflictMessage(authenticationType, apiKeyLocation, apiKeyHeaderName));
        }

        var conflictingOperationHeaderExists = await (
            from header in dbContext.Headers
            join operation in dbContext.Operations on header.OperationId equals operation.Id
            where header.Id != ignoredHeaderId
                && header.IsEnabled
                && operation.DataSourceId == dataSourceId
                && header.Key.ToUpper() == effectiveHeaderNameUpper
            select header.Id)
            .AnyAsync(cancellationToken);

        if (conflictingOperationHeaderExists)
        {
            throw new InvalidOperationException(
                DataSourceAuthenticationRules.CreateConflictMessage(authenticationType, apiKeyLocation, apiKeyHeaderName));
        }
    }

    public static async Task EnsureNoEnabledQueryParameterConflictAsync(
        IAppDbContext dbContext,
        Guid dataSourceId,
        AuthenticationType authenticationType,
        ApiKeyLocation? apiKeyLocation,
        string? apiKeyHeaderName,
        CancellationToken cancellationToken)
    {
        if (authenticationType != AuthenticationType.ApiKey
            || DataSourceAuthenticationRules.NormalizeApiKeyLocation(apiKeyLocation) != ApiKeyLocation.Query)
        {
            return;
        }

        var queryParameterKeys = await (
            from queryParameter in dbContext.QueryParameters
            join operation in dbContext.Operations on queryParameter.OperationId equals operation.Id
            where queryParameter.IsEnabled && operation.DataSourceId == dataSourceId
            select queryParameter.Key)
            .ToListAsync(cancellationToken);

        if (queryParameterKeys.Any(x => string.Equals(x, apiKeyHeaderName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                DataSourceAuthenticationRules.CreateQueryConflictMessage(apiKeyHeaderName!));
        }
    }

    public static async Task EnsureHeaderDoesNotConflictAsync(
        IAppDbContext dbContext,
        Guid dataSourceId,
        string headerKey,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        if (!isEnabled)
        {
            return;
        }

        var authentication = await dbContext.DataSourceAuthentications
            .AsNoTracking()
            .Where(x => x.DataSourceId == dataSourceId)
            .Select(x => new
            {
                x.AuthenticationType,
                x.ApiKeyLocation,
                x.ApiKeyHeaderName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (authentication is null)
        {
            return;
        }

        if (DataSourceAuthenticationRules.ConflictsWithHeader(
            authentication.AuthenticationType,
            authentication.ApiKeyLocation,
            authentication.ApiKeyHeaderName,
            headerKey))
        {
            throw new InvalidOperationException(
                DataSourceAuthenticationRules.CreateConflictMessage(
                    authentication.AuthenticationType,
                    authentication.ApiKeyLocation,
                    authentication.ApiKeyHeaderName));
        }
    }

    public static async Task EnsureQueryParameterDoesNotConflictAsync(
        IAppDbContext dbContext,
        Guid operationId,
        string queryParameterKey,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        if (!isEnabled)
        {
            return;
        }

        var authentication = await (
            from operation in dbContext.Operations
            join authenticationCandidate in dbContext.DataSourceAuthentications on operation.DataSourceId equals authenticationCandidate.DataSourceId
            where operation.Id == operationId
            select new
            {
                authenticationCandidate.AuthenticationType,
                authenticationCandidate.ApiKeyLocation,
                authenticationCandidate.ApiKeyHeaderName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (authentication is null)
        {
            return;
        }

        if (DataSourceAuthenticationRules.ConflictsWithQueryParameter(
            authentication.AuthenticationType,
            authentication.ApiKeyLocation,
            authentication.ApiKeyHeaderName,
            queryParameterKey))
        {
            throw new InvalidOperationException(
                DataSourceAuthenticationRules.CreateQueryConflictMessage(queryParameterKey));
        }
    }
}
