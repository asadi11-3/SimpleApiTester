using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Persistence;

namespace SimpleApiTester.Application.Operations.Commands.UpdateOperation;

internal sealed class UpdateOperationCommandHandler
    : IRequestHandler<UpdateOperationCommand>
{
    private readonly IAppDbContext _dbContext;

    public UpdateOperationCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        UpdateOperationCommand request,
        CancellationToken cancellationToken)
    {
        var operation = await _dbContext.Operations
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (operation is null)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        var dataSourceExists = await _dbContext.DataSources
            .AnyAsync(x => x.Id == request.DataSourceId, cancellationToken);

        if (!dataSourceExists)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        operation.DataSourceId = request.DataSourceId;
        operation.ApiName = request.ApiName.Trim();
        operation.Endpoint = NormalizeEndpoint(request.Endpoint);
        operation.MethodType = request.MethodType;
        operation.Body = request.Body;
        operation.ContentType = ContentTypeRules.Normalize(request.ContentType);
        operation.AuthenticationMode = request.AuthenticationMode;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        var trimmedEndpoint = endpoint.Trim();

        return "/" + trimmedEndpoint.TrimStart('/');
    }
}
