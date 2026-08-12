using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Operations.Commands.DeleteOperation;

internal sealed class DeleteOperationCommandHandler
    : IRequestHandler<DeleteOperationCommand>
{
    private readonly IAppDbContext _dbContext;

    public DeleteOperationCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        DeleteOperationCommand request,
        CancellationToken cancellationToken)
    {
        var operation = await _dbContext.Operations
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (operation is null)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        _dbContext.Operations.Remove(operation);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
