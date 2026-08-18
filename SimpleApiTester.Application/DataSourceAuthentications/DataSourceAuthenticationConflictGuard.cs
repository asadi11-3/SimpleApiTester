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
        string? apiKeyHeaderName,
        CancellationToken cancellationToken,
        Guid? ignoredHeaderId = null)
    {
        var effectiveHeaderNameUpper = DataSourceAuthenticationRules
            .GetEffectiveHeaderName(authenticationType, apiKeyHeaderName)
            .ToUpperInvariant();

        var conflictingDataSourceHeaderExists = await dbContext.Headers.AnyAsync(
            x => x.Id != ignoredHeaderId
                && x.IsEnabled
                && x.DataSourceId == dataSourceId
                && x.Key.ToUpper() == effectiveHeaderNameUpper,
            cancellationToken);

        if (conflictingDataSourceHeaderExists)
        {
            throw new InvalidOperationException(
                DataSourceAuthenticationRules.CreateConflictMessage(authenticationType, apiKeyHeaderName));
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
                DataSourceAuthenticationRules.CreateConflictMessage(authenticationType, apiKeyHeaderName));
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
                x.ApiKeyHeaderName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (authentication is null)
        {
            return;
        }

        if (DataSourceAuthenticationRules.ConflictsWithHeader(
            authentication.AuthenticationType,
            authentication.ApiKeyHeaderName,
            headerKey))
        {
            throw new InvalidOperationException(
                DataSourceAuthenticationRules.CreateConflictMessage(
                    authentication.AuthenticationType,
                    authentication.ApiKeyHeaderName));
        }
    }
}
