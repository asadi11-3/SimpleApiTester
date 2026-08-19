using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.DataSourceAuthentications;
using SimpleApiTester.Application.Abstractions.Headers;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Application.DataSources;
using SimpleApiTester.Domain.Entities;
using SimpleApiTester.Domain.Enum;

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
                x.ContentType,
                x.AuthenticationMode
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
                x.IsActive,
                x.DefaultTimeoutSeconds
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dataSource is null)
        {
            throw new KeyNotFoundException("DataSource not found.");
        }

        var environment = await _dbContext.DataSourceEnvironments
            .AsNoTracking()
            .Where(x => x.Id == request.EnvironmentId)
            .Select(x => new
            {
                x.DataSourceId,
                x.BaseUrl,
                x.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (environment is null)
        {
            throw new KeyNotFoundException("Environment not found.");
        }

        if (environment.DataSourceId != operation.DataSourceId)
        {
            throw new InvalidOperationException("The selected environment does not belong to the operation's data source.");
        }

        if (!dataSource.IsActive)
        {
            throw new InvalidOperationException("Cannot execute an operation for an inactive data source.");
        }

        if (!environment.IsActive)
        {
            throw new InvalidOperationException("Cannot execute an operation for an inactive environment.");
        }

        var queryParameters = await _dbContext.QueryParameters
            .AsNoTracking()
            .Where(x => x.OperationId == operation.Id && x.IsEnabled)
            .Select(x => new QueryParameterValue(x.Key, x.Value))
            .ToListAsync(cancellationToken);

        var rawHeaderResolution = await ResolveHeadersAsync(
            operation.Id,
            operation.DataSourceId,
            request.EnvironmentId,
            cancellationToken);

        if (rawHeaderResolution.ErrorResponse is not null)
        {
            return rawHeaderResolution.ErrorResponse;
        }

        var finalHeaders = rawHeaderResolution.Headers.ToList();

        if (operation.AuthenticationMode == OperationAuthenticationMode.Inherit)
        {
            var authentication = await _dbContext.DataSourceAuthentications
                .AsNoTracking()
                .Where(x => x.DataSourceId == operation.DataSourceId)
                .Select(x => new DataSourceAuthenticationValue(
                    x.AuthenticationType,
                    x.ValueSourceType,
                    x.SourceKey,
                    x.ApiKeyHeaderName))
                .FirstOrDefaultAsync(cancellationToken);

            if (authentication is not null)
            {
                var effectiveHeaderName = DataSourceAuthenticationRules.GetEffectiveHeaderName(
                    authentication.AuthenticationType,
                    authentication.ApiKeyHeaderName);

                if (rawHeaderResolution.MergedHeaderKeys.Any(
                    x => string.Equals(x, effectiveHeaderName, StringComparison.OrdinalIgnoreCase)))
                {
                    return CreateAuthenticationConfigurationErrorResponse(
                        authentication.AuthenticationType,
                        authentication.ApiKeyHeaderName);
                }

                var resolvedAuthenticationValue = await ResolveAuthenticationValueAsync(
                    authentication,
                    rawHeaderResolution.EnabledVariables,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(resolvedAuthenticationValue))
                {
                    return CreateAuthenticationResolutionErrorResponse(
                        authentication.AuthenticationType,
                        authentication.SourceKey);
                }

                finalHeaders.Add(new ResolvedRequestHeader(
                    effectiveHeaderName,
                    authentication.AuthenticationType == AuthenticationType.Bearer
                        ? $"Bearer {resolvedAuthenticationValue}"
                        : resolvedAuthenticationValue));
            }
        }

        if (!TryBuildRequestUrl(environment.BaseUrl, operation.Endpoint, queryParameters, out var requestUrl))
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
                finalHeaders),
            DataSourceTimeoutPolicy.Resolve(dataSource.DefaultTimeoutSeconds),
            cancellationToken);
    }

    private async Task<HeaderResolutionResult> ResolveHeadersAsync(
        Guid operationId,
        Guid dataSourceId,
        Guid dataSourceEnvironmentId,
        CancellationToken cancellationToken)
    {
        var enabledVariables = await _dbContext.Variables
            .AsNoTracking()
            .Where(x => x.DataSourceEnvironmentId == dataSourceEnvironmentId && x.IsEnabled)
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
                    mergedHeaders.Keys.ToList(),
                    enabledVariables,
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

        return new HeaderResolutionResult(
            resolvedHeaders,
            mergedHeaders.Keys.ToList(),
            enabledVariables,
            null);
    }

    private async Task<string?> ResolveAuthenticationValueAsync(
        DataSourceAuthenticationValue authentication,
        IReadOnlyCollection<VariableValue> enabledVariables,
        CancellationToken cancellationToken)
    {
        return authentication.ValueSourceType switch
        {
            HeaderValueSourceType.Variable => ResolveVariableValue(authentication.SourceKey, enabledVariables),
            HeaderValueSourceType.UserSecret => await _externalHeaderValueResolver.ResolveAsync(
                HeaderValueSourceType.UserSecret,
                authentication.SourceKey,
                cancellationToken),
            HeaderValueSourceType.EnvironmentVariable => await _externalHeaderValueResolver.ResolveAsync(
                HeaderValueSourceType.EnvironmentVariable,
                authentication.SourceKey,
                cancellationToken),
            _ => null
        };
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
                $"Header '{header.Key}' references variable '{header.SourceKey}', but no enabled variable with that key was found for the selected environment.",
            HeaderValueSourceType.UserSecret =>
                $"Header '{header.Key}' could not resolve configuration value '{header.SourceKey}'.",
            HeaderValueSourceType.EnvironmentVariable =>
                $"Header '{header.Key}' could not resolve environment variable '{header.SourceKey}'.",
            _ => $"Header '{header.Key}' could not be resolved."
        };
    }

    private static ExecuteOperationResponse CreateAuthenticationResolutionErrorResponse(
        AuthenticationType authenticationType,
        string sourceKey)
        => new(
            StatusCode: null,
            IsSuccessStatusCode: null,
            ResponseBody: null,
            ContentType: null,
            DurationMilliseconds: 0,
            HasExecutionError: true,
            ErrorType: "AuthenticationResolutionError",
            ErrorMessage: DataSourceAuthenticationRules.CreateResolutionErrorMessage(authenticationType, sourceKey));

    private static ExecuteOperationResponse CreateAuthenticationConfigurationErrorResponse(
        AuthenticationType authenticationType,
        string? apiKeyHeaderName)
        => new(
            StatusCode: null,
            IsSuccessStatusCode: null,
            ResponseBody: null,
            ContentType: null,
            DurationMilliseconds: 0,
            HasExecutionError: true,
            ErrorType: "AuthenticationConfigurationError",
            ErrorMessage: DataSourceAuthenticationRules.CreateConflictMessage(authenticationType, apiKeyHeaderName));

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
        IReadOnlyCollection<string> MergedHeaderKeys,
        IReadOnlyCollection<VariableValue> EnabledVariables,
        ExecuteOperationResponse? ErrorResponse);

    private sealed record DataSourceAuthenticationValue(
        AuthenticationType AuthenticationType,
        HeaderValueSourceType ValueSourceType,
        string SourceKey,
        string? ApiKeyHeaderName);
}
