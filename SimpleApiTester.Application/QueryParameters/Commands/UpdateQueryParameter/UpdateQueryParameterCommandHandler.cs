using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.QueryParameters.Commands.UpdateQueryParameter;

internal sealed class UpdateQueryParameterCommandHandler
    : IRequestHandler<UpdateQueryParameterCommand>
{
    private readonly IAppDbContext _dbContext;

    public UpdateQueryParameterCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        UpdateQueryParameterCommand request,
        CancellationToken cancellationToken)
    {
        var queryParameter = await _dbContext.QueryParameters
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (queryParameter is null)
        {
            throw new KeyNotFoundException("QueryParameter not found.");
        }

        var operationExists = await _dbContext.Operations
            .AnyAsync(x => x.Id == request.OperationId, cancellationToken);

        if (!operationExists)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        queryParameter.OperationId = request.OperationId;
        queryParameter.Key = request.Key.Trim();
        queryParameter.Value = request.Value;
        queryParameter.IsEnabled = request.IsEnabled;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
