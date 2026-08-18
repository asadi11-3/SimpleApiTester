using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Application.DataSourceAuthentications;

namespace SimpleApiTester.Application.Headers.Commands.UpdateHeader;

internal sealed class UpdateHeaderCommandHandler : IRequestHandler<UpdateHeaderCommand>
{
    private readonly IAppDbContext _dbContext;

    public UpdateHeaderCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateHeaderCommand request, CancellationToken cancellationToken)
    {
        var header = await _dbContext.Headers
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (header is null)
        {
            throw new KeyNotFoundException("Header not found.");
        }

        var normalizedKey = HeaderRules.NormalizeKey(request.Key);
        var normalizedKeyUpper = normalizedKey.ToUpperInvariant();
        var normalizedSourceKey = HeaderRules.NormalizeSourceKey(request.SourceKey);

        var dataSourceId = header.DataSourceId;
        var operationId = header.OperationId;

        var duplicateExists = await _dbContext.Headers.AnyAsync(
            x => x.Id != request.Id
                && ((dataSourceId != null && x.DataSourceId == dataSourceId)
                    || (operationId != null && x.OperationId == operationId))
                && x.Key.ToUpper() == normalizedKeyUpper,
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Header with key '{normalizedKey}' already exists in this scope.");
        }

        var owningDataSourceId = dataSourceId ?? await _dbContext.Operations
            .AsNoTracking()
            .Where(x => x.Id == operationId)
            .Select(x => (Guid?)x.DataSourceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (owningDataSourceId is not null)
        {
            await DataSourceAuthenticationConflictGuard.EnsureHeaderDoesNotConflictAsync(
                _dbContext,
                owningDataSourceId.Value,
                normalizedKey,
                request.IsEnabled,
                cancellationToken);
        }

        header.Key = normalizedKey;
        header.ValueSourceType = request.ValueSourceType;
        header.Value = request.ValueSourceType == Domain.Enum.HeaderValueSourceType.General ? request.Value : null;
        header.SourceKey = request.ValueSourceType == Domain.Enum.HeaderValueSourceType.General ? null : normalizedSourceKey;
        header.IsEnabled = request.IsEnabled;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
