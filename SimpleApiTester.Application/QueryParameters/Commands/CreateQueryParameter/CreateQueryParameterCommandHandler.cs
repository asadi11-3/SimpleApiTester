using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.QueryParameters.Commands.CreateQueryParameter;

internal sealed class CreateQueryParameterCommandHandler
    : IRequestHandler<CreateQueryParameterCommand, Guid>
{
    private readonly IAppDbContext _dbContext;

    public CreateQueryParameterCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(
        CreateQueryParameterCommand request,
        CancellationToken cancellationToken)
    {
        var operationExists = await _dbContext.Operations
            .AnyAsync(x => x.Id == request.OperationId, cancellationToken);

        if (!operationExists)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        var queryParameter = new QueryParameter
        {
            Id = Guid.NewGuid(),
            OperationId = request.OperationId,
            Key = request.Key.Trim(),
            Value = request.Value,
            IsEnabled = request.IsEnabled
        };

        _dbContext.QueryParameters.Add(queryParameter);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return queryParameter.Id;
    }
}
