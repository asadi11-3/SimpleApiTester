using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Operations.Queries.GetOperationById;

internal sealed class GetOperationByIdQueryHandler
    : IRequestHandler<GetOperationByIdQuery, OperationResponse>
{
    private readonly IAppDbContext _dbContext;

    public GetOperationByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OperationResponse> Handle(
        GetOperationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var operation = await _dbContext.Operations
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new OperationResponse(
                x.Id,
                x.DataSourceId,
                x.ApiName,
                x.Endpoint,
                x.MethodType,
                x.Body,
                x.ContentType,
                x.AuthenticationMode))
            .FirstOrDefaultAsync(cancellationToken);

        if (operation is null)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        return operation;
    }
}
