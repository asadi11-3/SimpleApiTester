using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Variables.Commands.DeleteVariable;

internal sealed class DeleteVariableCommandHandler : IRequestHandler<DeleteVariableCommand>
{
    private readonly IAppDbContext _dbContext;

    public DeleteVariableCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteVariableCommand request, CancellationToken cancellationToken)
    {
        var variable = await _dbContext.Variables
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (variable is null)
        {
            throw new KeyNotFoundException("Variable not found.");
        }

        _dbContext.Variables.Remove(variable);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
