using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Headers.Commands.CreateOperationHeader;

internal sealed class CreateOperationHeaderCommandHandler : IRequestHandler<CreateOperationHeaderCommand, Guid>
{
    private readonly IAppDbContext _dbContext;

    public CreateOperationHeaderCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateOperationHeaderCommand request, CancellationToken cancellationToken)
    {
        var operationExists = await _dbContext.Operations
            .AnyAsync(x => x.Id == request.OperationId, cancellationToken);

        if (!operationExists)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        var header = await HeaderFactory.CreateOperationHeaderAsync(_dbContext, request, cancellationToken);

        _dbContext.Headers.Add(header);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return header.Id;
    }
}
