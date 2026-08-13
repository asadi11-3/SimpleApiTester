using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.QueryParameters.Commands.DeleteQueryParameter;

internal sealed class DeleteQueryParameterCommandHandler
    : IRequestHandler<DeleteQueryParameterCommand>
{
    private readonly IAppDbContext _dbContext;

    public DeleteQueryParameterCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        DeleteQueryParameterCommand request,
        CancellationToken cancellationToken)
    {
        var queryParameter = await _dbContext.QueryParameters
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (queryParameter is null)
        {
            throw new KeyNotFoundException("QueryParameter not found.");
        }

        _dbContext.QueryParameters.Remove(queryParameter);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
