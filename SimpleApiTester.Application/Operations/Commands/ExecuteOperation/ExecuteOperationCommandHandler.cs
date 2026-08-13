using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.Operations.Commands.ExecuteOperation;

internal sealed class ExecuteOperationCommandHandler
    : IRequestHandler<ExecuteOperationCommand, ExecuteOperationResponse>
{
    private readonly IAppDbContext _dbContext;
    private readonly IOperationRequestExecutor _operationRequestExecutor;

    public ExecuteOperationCommandHandler(
        IAppDbContext dbContext,
        IOperationRequestExecutor operationRequestExecutor)
    {
        _dbContext = dbContext;
        _operationRequestExecutor = operationRequestExecutor;
    }

    public async Task<ExecuteOperationResponse> Handle(
        ExecuteOperationCommand request,
        CancellationToken cancellationToken)
    {
        var operation = await _dbContext.Operations
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new
            {
                x.Id,
                x.DataSourceId,
                x.Endpoint,
                x.MethodType,
                x.Body,
                x.ContentType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (operation is null)
        {
            throw new KeyNotFoundException("Operation not found.");
        }

        var dataSource = await _dbContext.DataSources
            .AsNoTracking()
            .Where(x => x.Id == operation.DataSourceId)
            .Select(x => new
            {
                x.BaseUrl,
                x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dataSource is null)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        if (!dataSource.IsActive)
        {
            throw new InvalidOperationException("Cannot execute an operation for an inactive data source.");
        }

        var queryParameters = await _dbContext.QueryParameters
            .AsNoTracking()
            .Where(x => x.OperationId == operation.Id && x.IsEnabled)
            .Select(x => new QueryParameterValue(x.Key, x.Value))
            .ToListAsync(cancellationToken);

        if (!TryBuildRequestUrl(dataSource.BaseUrl, operation.Endpoint, queryParameters, out var requestUrl))
        {
            return new ExecuteOperationResponse(
                StatusCode: null,
                IsSuccessStatusCode: null,
                ResponseBody: null,
                ContentType: null,
                DurationMilliseconds: 0,
                HasExecutionError: true,
                ErrorType: "InvalidUrl",
                ErrorMessage: "The combined BaseUrl and Endpoint do not form a valid absolute HTTP/HTTPS URL.");
        }

        return await _operationRequestExecutor.ExecuteAsync(
            new OperationHttpRequest(
                requestUrl,
                operation.MethodType,
                operation.Body,
                operation.ContentType),
            cancellationToken);
    }

    private static bool TryBuildRequestUrl(
        string baseUrl,
        string endpoint,
        IReadOnlyCollection<QueryParameterValue> queryParameters,
        out string requestUrl)
    {
        requestUrl = string.Empty;

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return false;
        }

        if (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var normalizedBaseUrl = baseUrl.TrimEnd('/') + "/";
        var normalizedEndpoint = endpoint.TrimStart('/');

        if (!Uri.TryCreate(new Uri(normalizedBaseUrl, UriKind.Absolute), normalizedEndpoint, out var combinedUri))
        {
            return false;
        }

        if (!combinedUri.IsAbsoluteUri)
        {
            return false;
        }

        if (combinedUri.Scheme != Uri.UriSchemeHttp && combinedUri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        requestUrl = BuildFinalUrl(combinedUri, queryParameters);
        return true;
    }

    private static string BuildFinalUrl(
        Uri uri,
        IReadOnlyCollection<QueryParameterValue> queryParameters)
    {
        if (queryParameters.Count == 0)
        {
            return uri.ToString();
        }

        var builder = new UriBuilder(uri)
        {
            Query = string.Join(
                "&",
                queryParameters.Select(x =>
                    $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"))
        };

        return builder.Uri.ToString();
    }

    private sealed record QueryParameterValue(string Key, string? Value);
}
