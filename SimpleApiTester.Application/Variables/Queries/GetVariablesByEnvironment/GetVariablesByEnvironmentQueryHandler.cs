using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Variables.Queries.GetVariablesByEnvironment;

internal sealed class GetVariablesByEnvironmentQueryHandler
    : IRequestHandler<GetVariablesByEnvironmentQuery, IReadOnlyList<VariableResponse>>
{
    private readonly IAppDbContext _dbContext;

    public GetVariablesByEnvironmentQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<VariableResponse>> Handle(
        GetVariablesByEnvironmentQuery request,
        CancellationToken cancellationToken)
    {
        var environmentExists = await _dbContext.DataSourceEnvironments
            .AnyAsync(x => x.Id == request.DataSourceEnvironmentId, cancellationToken);

        if (!environmentExists)
        {
            throw new KeyNotFoundException("Environment not found.");
        }

        return await _dbContext.Variables
            .AsNoTracking()
            .Where(x => x.DataSourceEnvironmentId == request.DataSourceEnvironmentId)
            .Select(x => new VariableResponse(
                x.Id,
                x.DataSourceEnvironmentId,
                x.Key,
                x.IsSecret ? VariableValueMasking.MaskedValue : x.Value,
                x.IsEnabled,
                x.IsSecret))
            .ToListAsync(cancellationToken);
    }
}
