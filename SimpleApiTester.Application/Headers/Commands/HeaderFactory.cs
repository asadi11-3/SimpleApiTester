using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Application.DataSourceAuthentications;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.Headers.Commands;

internal static class HeaderFactory
{
    public static async Task<Header> CreateDataSourceHeaderAsync(
        IAppDbContext dbContext,
        CreateDataSourceHeader.CreateDataSourceHeaderCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedKey = HeaderRules.NormalizeKey(request.Key);
        var normalizedKeyUpper = normalizedKey.ToUpperInvariant();

        var duplicateExists = await dbContext.Headers
            .AnyAsync(
                x => x.DataSourceId == request.DataSourceId && x.Key.ToUpper() == normalizedKeyUpper,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Header with key '{normalizedKey}' already exists in this scope.");
        }

        await DataSourceAuthenticationConflictGuard.EnsureHeaderDoesNotConflictAsync(
            dbContext,
            request.DataSourceId,
            normalizedKey,
            request.IsEnabled,
            cancellationToken);

        return CreateHeader(
            dataSourceId: request.DataSourceId,
            operationId: null,
            normalizedKey,
            request.ValueSourceType,
            request.Value,
            HeaderRules.NormalizeSourceKey(request.SourceKey),
            request.IsEnabled);
    }

    public static async Task<Header> CreateOperationHeaderAsync(
        IAppDbContext dbContext,
        CreateOperationHeader.CreateOperationHeaderCommand request,
        Guid dataSourceId,
        CancellationToken cancellationToken)
    {
        var normalizedKey = HeaderRules.NormalizeKey(request.Key);
        var normalizedKeyUpper = normalizedKey.ToUpperInvariant();

        var duplicateExists = await dbContext.Headers
            .AnyAsync(
                x => x.OperationId == request.OperationId && x.Key.ToUpper() == normalizedKeyUpper,
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Header with key '{normalizedKey}' already exists in this scope.");
        }

        await DataSourceAuthenticationConflictGuard.EnsureHeaderDoesNotConflictAsync(
            dbContext,
            dataSourceId,
            normalizedKey,
            request.IsEnabled,
            cancellationToken);

        return CreateHeader(
            dataSourceId: null,
            operationId: request.OperationId,
            normalizedKey,
            request.ValueSourceType,
            request.Value,
            HeaderRules.NormalizeSourceKey(request.SourceKey),
            request.IsEnabled);
    }

    private static Header CreateHeader(
        Guid? dataSourceId,
        Guid? operationId,
        string normalizedKey,
        Domain.Enum.HeaderValueSourceType valueSourceType,
        string? value,
        string? normalizedSourceKey,
        bool isEnabled)
    {
        return new Header
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            OperationId = operationId,
            Key = normalizedKey,
            ValueSourceType = valueSourceType,
            Value = valueSourceType == Domain.Enum.HeaderValueSourceType.General ? value : null,
            SourceKey = valueSourceType == Domain.Enum.HeaderValueSourceType.General ? null : normalizedSourceKey,
            IsEnabled = isEnabled
        };
    }
}
