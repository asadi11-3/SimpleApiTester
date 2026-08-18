using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Headers;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Domain.Enum;
using SimpleApiTester.Domain.Entities;

namespace SimpleApiTester.Application.Operations.Commands.ExecuteOperation;

internal sealed class ExecuteOperationCommandHandler
    : IRequestHandler<ExecuteOperationCommand, ExecuteOperationResponse>
{
    private readonly IAppDbContext _dbContext;
    private readonly IExternalHeaderValueResolver _externalHeaderValueResolver;
    private readonly IOperationRequestExecutor _operationRequestExecutor;

    public ExecuteOperationCommandHandler(
        IAppDbContext dbContext,
        IExternalHeaderValueResolver externalHeaderValueResolver,
        IOperationRequestExecutor operationRequestExecutor)
    {
        _dbContext = dbContext;
        _externalHeaderValueResolver = externalHeaderValueResolver;
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

        var resolvedHeaders = await ResolveHeadersAsync(
            operation.Id,
            operation.DataSourceId,
            cancellationToken);

        if (resolvedHeaders.ErrorResponse is not null)
        {
            return resolvedHeaders.ErrorResponse;
        }

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
                operation.ContentType,
                resolvedHeaders.Headers),
            cancellationToken);
    }

    private async Task<HeaderResolutionResult> ResolveHeadersAsync(
        Guid operationId,
        Guid dataSourceId,
        CancellationToken cancellationToken)
    {
        var enabledVariables = await _dbContext.Variables
            .AsNoTracking()
            .Where(x => x.DataSourceId == dataSourceId && x.IsEnabled)
            .Select(x => new VariableValue(x.Key, x.Value))
            .ToListAsync(cancellationToken);

        var enabledDataSourceHeaders = await _dbContext.Headers
            .AsNoTracking()
            .Where(x => x.DataSourceId == dataSourceId && x.IsEnabled)
            .Select(x => new HeaderValue(x.Key, x.ValueSourceType, x.Value, x.SourceKey))
            .ToListAsync(cancellationToken);

        var enabledOperationHeaders = await _dbContext.Headers
            .AsNoTracking()
            .Where(x => x.OperationId == operationId && x.IsEnabled)
            .Select(x => new HeaderValue(x.Key, x.ValueSourceType, x.Value, x.SourceKey))
            .ToListAsync(cancellationToken);

        var mergedHeaders = new Dictionary<string, HeaderValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in enabledDataSourceHeaders)
        {
            mergedHeaders[header.Key] = header;
        }

        foreach (var header in enabledOperationHeaders)
        {
            mergedHeaders[header.Key] = header;
        }

        var resolvedHeaders = new List<ResolvedRequestHeader>(mergedHeaders.Count);

        foreach (var header in mergedHeaders.Values)
        {
            var resolvedValue = await ResolveHeaderValueAsync(
                header,
                enabledVariables,
                cancellationToken);

            if (resolvedValue is null)
            {
                return new HeaderResolutionResult(
                    [],
                    new ExecuteOperationResponse(
                        StatusCode: null,
                        IsSuccessStatusCode: null,
                        ResponseBody: null,
                        ContentType: null,
                        DurationMilliseconds: 0,
                        HasExecutionError: true,
                        ErrorType: "HeaderResolutionError",
                        ErrorMessage: CreateHeaderResolutionErrorMessage(header)));
            }

            resolvedHeaders.Add(new ResolvedRequestHeader(header.Key, resolvedValue));
        }

        return new HeaderResolutionResult(resolvedHeaders, null);
    }

    private async Task<string?> ResolveHeaderValueAsync(
        HeaderValue header,
        IReadOnlyCollection<VariableValue> enabledVariables,
        CancellationToken cancellationToken)
    {
        return header.ValueSourceType switch
        {
            HeaderValueSourceType.General => header.Value,
            HeaderValueSourceType.Variable => ResolveVariableValue(header.SourceKey!, enabledVariables),
            HeaderValueSourceType.UserSecret => await _externalHeaderValueResolver.ResolveAsync(
                HeaderValueSourceType.UserSecret,
                header.SourceKey!,
                cancellationToken),
            HeaderValueSourceType.EnvironmentVariable => await _externalHeaderValueResolver.ResolveAsync(
                HeaderValueSourceType.EnvironmentVariable,
                header.SourceKey!,
                cancellationToken),
            _ => null
        };
    }

    private static string? ResolveVariableValue(
        string sourceKey,
        IReadOnlyCollection<VariableValue> enabledVariables)
    {
        var variable = enabledVariables.FirstOrDefault(
            x => string.Equals(x.Key, sourceKey, StringComparison.OrdinalIgnoreCase));

        return variable?.Value;
    }

    private static string CreateHeaderResolutionErrorMessage(HeaderValue header)
    {
        return header.ValueSourceType switch
        {
            HeaderValueSourceType.Variable =>
                $"Header '{header.Key}' references variable '{header.SourceKey}', but no enabled variable with that key was found for the data source.",
            HeaderValueSourceType.UserSecret =>
                $"Header '{header.Key}' could not resolve configuration value '{header.SourceKey}'.",
            HeaderValueSourceType.EnvironmentVariable =>
                $"Header '{header.Key}' could not resolve environment variable '{header.SourceKey}'.",
            _ => $"Header '{header.Key}' could not be resolved."
        };
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
        var requestPath = uri.GetLeftPart(UriPartial.Path);

        if (queryParameters.Count == 0)
        {
            return requestPath;
        }

        var queryString = string.Join(
            "&",
            queryParameters.Select(x =>
                $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"));

        return $"{requestPath}?{queryString}";
    }

    private sealed record QueryParameterValue(string Key, string? Value);

    private sealed record VariableValue(string Key, string? Value);

    private sealed record HeaderValue(
        string Key,
        HeaderValueSourceType ValueSourceType,
        string? Value,
        string? SourceKey);

    private sealed record HeaderResolutionResult(
        IReadOnlyCollection<ResolvedRequestHeader> Headers,
        ExecuteOperationResponse? ErrorResponse);
}
