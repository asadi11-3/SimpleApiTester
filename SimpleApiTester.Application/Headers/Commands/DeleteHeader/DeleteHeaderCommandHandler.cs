using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Headers.Commands.DeleteHeader;

internal sealed class DeleteHeaderCommandHandler : IRequestHandler<DeleteHeaderCommand>
{
    private readonly IAppDbContext _dbContext;

    public DeleteHeaderCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteHeaderCommand request, CancellationToken cancellationToken)
    {
        var header = await _dbContext.Headers
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (header is null)
        {
            throw new KeyNotFoundException("Header not found.");
        }

        _dbContext.Headers.Remove(header);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
