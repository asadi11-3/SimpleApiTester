using MediatR;
using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Authentication;
using SimpleApiTester.Application.DataSourceAuthentications;
using SimpleApiTester.Application.Abstractions.Headers;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Application.DataSources;
using SimpleApiTester.Domain.Entities;
using SimpleApiTester.Domain.Enum;
using System.Text;

namespace SimpleApiTester.Application.Operations.Commands.ExecuteOperation;

internal sealed class ExecuteOperationCommandHandler
    : IRequestHandler<ExecuteOperationCommand, ExecuteOperationResponse>
{
    private readonly IAppDbContext _dbContext;
    private readonly IExternalHeaderValueResolver _externalHeaderValueResolver;
    private readonly IOAuthTokenClient _oauthTokenClient;
    private readonly IOperationRequestExecutor _operationRequestExecutor;

    public ExecuteOperationCommandHandler(
        IAppDbContext dbContext,
        IExternalHeaderValueResolver externalHeaderValueResolver,
        IOAuthTokenClient oauthTokenClient,
        IOperationRequestExecutor operationRequestExecutor)
    {
        _dbContext = dbContext;
        _externalHeaderValueResolver = externalHeaderValueResolver;
        _oauthTokenClient = oauthTokenClient;
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

        var timeout = DataSourceTimeoutPolicy.Resolve(dataSource.DefaultTimeoutSeconds);

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
        var finalQueryParameters = queryParameters.ToList();

        if (operation.AuthenticationMode == OperationAuthenticationMode.Inherit)
        {
            var authentication = await _dbContext.DataSourceAuthentications
                .AsNoTracking()
                .Where(x => x.DataSourceId == operation.DataSourceId)
                .Select(x => new DataSourceAuthenticationValue(
                    x.AuthenticationType,
                    x.ValueSourceType,
                    x.SourceKey,
                    x.ApiKeyHeaderName,
                    x.ApiKeyLocation,
                    x.UsernameSourceType,
                    x.UsernameSourceKey,
                    x.PasswordSourceType,
                    x.PasswordSourceKey,
                    x.OAuthTokenEndpoint,
                    x.OAuthClientIdSourceType,
                    x.OAuthClientIdSourceKey,
                    x.OAuthClientSecretSourceType,
                    x.OAuthClientSecretSourceKey,
                    x.OAuthScope))
                .FirstOrDefaultAsync(cancellationToken);

            if (authentication is not null)
            {
                if (authentication.AuthenticationType == AuthenticationType.ApiKey
                    && DataSourceAuthenticationRules.NormalizeApiKeyLocation(authentication.ApiKeyLocation) == ApiKeyLocation.Query)
                {
                    if (finalQueryParameters.Any(x => DataSourceAuthenticationRules.ConflictsWithQueryParameter(
                        authentication.AuthenticationType,
                        authentication.ApiKeyLocation,
                        authentication.ApiKeyHeaderName,
                        x.Key)))
                    {
                        return CreateAuthenticationConfigurationErrorResponse(
                            DataSourceAuthenticationRules.CreateQueryConflictMessage(authentication.ApiKeyHeaderName!));
                    }
                }

                var effectiveHeaderName = DataSourceAuthenticationRules.GetEffectiveHeaderName(
                    authentication.AuthenticationType,
                    authentication.ApiKeyLocation,
                    authentication.ApiKeyHeaderName);

                if (effectiveHeaderName is not null
                    && rawHeaderResolution.MergedHeaderKeys.Any(
                        x => string.Equals(x, effectiveHeaderName, StringComparison.OrdinalIgnoreCase)))
                {
                    return CreateAuthenticationConfigurationErrorResponse(
                        DataSourceAuthenticationRules.CreateConflictMessage(
                            authentication.AuthenticationType,
                            authentication.ApiKeyLocation,
                            authentication.ApiKeyHeaderName));
                }

                var resolvedAuthenticationValue = await ResolveAuthenticationValueAsync(
                    authentication,
                    rawHeaderResolution.EnabledVariables,
                    timeout,
                    cancellationToken);

                if (resolvedAuthenticationValue.ErrorType is not null)
                {
                    return CreateExecutionErrorResponse(
                        resolvedAuthenticationValue.ErrorType,
                        resolvedAuthenticationValue.ErrorMessage!);
                }

                if (authentication.AuthenticationType == AuthenticationType.ApiKey
                    && DataSourceAuthenticationRules.NormalizeApiKeyLocation(authentication.ApiKeyLocation) == ApiKeyLocation.Query)
                {
                    finalQueryParameters.Add(new QueryParameterValue(
                        authentication.ApiKeyHeaderName!,
                        resolvedAuthenticationValue.ResolvedValue!));
                }
                else
                {
                    finalHeaders.Add(new ResolvedRequestHeader(
                        effectiveHeaderName!,
                        resolvedAuthenticationValue.ResolvedValue!));
                }
            }
        }

        if (!TryBuildRequestUrl(environment.BaseUrl, operation.Endpoint, finalQueryParameters, out var requestUrl))
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
            timeout,
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

    private async Task<AuthenticationValueResolutionResult> ResolveAuthenticationValueAsync(
        DataSourceAuthenticationValue authentication,
        IReadOnlyCollection<VariableValue> enabledVariables,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (authentication.AuthenticationType == AuthenticationType.OAuthClientCredentials)
        {
            if (string.IsNullOrWhiteSpace(authentication.OAuthTokenEndpoint)
                || !DataSourceAuthenticationRules.IsValidOAuthTokenEndpoint(authentication.OAuthTokenEndpoint))
            {
                return new AuthenticationValueResolutionResult(
                    null,
                    "AuthenticationConfigurationError",
                    DataSourceAuthenticationRules.CreateOAuthTokenEndpointConfigurationErrorMessage());
            }

            var resolvedClientId = await ResolveAuthenticationSourceValueAsync(
                authentication.OAuthClientIdSourceType,
                authentication.OAuthClientIdSourceKey,
                enabledVariables,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(resolvedClientId))
            {
                return new AuthenticationValueResolutionResult(
                    null,
                    "AuthenticationResolutionError",
                    DataSourceAuthenticationRules.CreateOAuthClientIdResolutionErrorMessage(
                        authentication.OAuthClientIdSourceKey!));
            }

            var resolvedClientSecret = await ResolveAuthenticationSourceValueAsync(
                authentication.OAuthClientSecretSourceType,
                authentication.OAuthClientSecretSourceKey,
                enabledVariables,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(resolvedClientSecret))
            {
                return new AuthenticationValueResolutionResult(
                    null,
                    "AuthenticationResolutionError",
                    DataSourceAuthenticationRules.CreateOAuthClientSecretResolutionErrorMessage(
                        authentication.OAuthClientSecretSourceKey!));
            }

            var tokenResult = await _oauthTokenClient.RequestClientCredentialsTokenAsync(
                new OAuthTokenRequest(
                    authentication.OAuthTokenEndpoint,
                    resolvedClientId,
                    resolvedClientSecret,
                    authentication.OAuthScope),
                timeout,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(tokenResult.AccessToken))
            {
                return new AuthenticationValueResolutionResult(
                    null,
                    "OAuthTokenError",
                    tokenResult.ErrorMessage ?? "OAuth token request failed.");
            }

            return new AuthenticationValueResolutionResult($"Bearer {tokenResult.AccessToken}", null, null);
        }

        if (authentication.AuthenticationType == AuthenticationType.Basic)
        {
            var resolvedUsername = await ResolveAuthenticationSourceValueAsync(
                authentication.UsernameSourceType,
                authentication.UsernameSourceKey,
                enabledVariables,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(resolvedUsername)
                || resolvedUsername.Contains(':'))
            {
                return new AuthenticationValueResolutionResult(
                    null,
                    "AuthenticationResolutionError",
                    DataSourceAuthenticationRules.CreateBasicUsernameResolutionErrorMessage(
                        authentication.UsernameSourceKey!));
            }

            var resolvedPassword = await ResolveAuthenticationSourceValueAsync(
                authentication.PasswordSourceType,
                authentication.PasswordSourceKey,
                enabledVariables,
                cancellationToken);

            if (resolvedPassword is null)
            {
                return new AuthenticationValueResolutionResult(
                    null,
                    "AuthenticationResolutionError",
                    DataSourceAuthenticationRules.CreateBasicPasswordResolutionErrorMessage(
                        authentication.PasswordSourceKey!));
            }

            var encodedCredentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{resolvedUsername}:{resolvedPassword}"));

            return new AuthenticationValueResolutionResult($"Basic {encodedCredentials}", null, null);
        }

        var resolvedAuthenticationValue = await ResolveAuthenticationSourceValueAsync(
            authentication.ValueSourceType,
            authentication.SourceKey,
            enabledVariables,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(resolvedAuthenticationValue))
        {
            return new AuthenticationValueResolutionResult(
                null,
                "AuthenticationResolutionError",
                DataSourceAuthenticationRules.CreateResolutionErrorMessage(
                    authentication.AuthenticationType,
                    authentication.SourceKey!));
        }

        return new AuthenticationValueResolutionResult(
            authentication.AuthenticationType == AuthenticationType.Bearer
                ? $"Bearer {resolvedAuthenticationValue}"
                : resolvedAuthenticationValue,
            null,
            null);
    }

    private async Task<string?> ResolveAuthenticationSourceValueAsync(
        HeaderValueSourceType? valueSourceType,
        string? sourceKey,
        IReadOnlyCollection<VariableValue> enabledVariables,
        CancellationToken cancellationToken)
    {
        if (valueSourceType is null || sourceKey is null)
        {
            return null;
        }

        return valueSourceType.Value switch
        {
            HeaderValueSourceType.Variable => ResolveVariableValue(sourceKey, enabledVariables),
            HeaderValueSourceType.UserSecret => await _externalHeaderValueResolver.ResolveAsync(
                HeaderValueSourceType.UserSecret,
                sourceKey,
                cancellationToken),
            HeaderValueSourceType.EnvironmentVariable => await _externalHeaderValueResolver.ResolveAsync(
                HeaderValueSourceType.EnvironmentVariable,
                sourceKey,
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

    private static ExecuteOperationResponse CreateExecutionErrorResponse(
        string errorType,
        string errorMessage)
        => new(
            StatusCode: null,
            IsSuccessStatusCode: null,
            ResponseBody: null,
            ContentType: null,
            DurationMilliseconds: 0,
            HasExecutionError: true,
            ErrorType: errorType,
            ErrorMessage: errorMessage);

    private static ExecuteOperationResponse CreateAuthenticationConfigurationErrorResponse(
        string errorMessage)
        => new(
            StatusCode: null,
            IsSuccessStatusCode: null,
            ResponseBody: null,
            ContentType: null,
            DurationMilliseconds: 0,
            HasExecutionError: true,
            ErrorType: "AuthenticationConfigurationError",
            ErrorMessage: errorMessage);

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
        HeaderValueSourceType? ValueSourceType,
        string? SourceKey,
        string? ApiKeyHeaderName,
        ApiKeyLocation? ApiKeyLocation,
        HeaderValueSourceType? UsernameSourceType,
        string? UsernameSourceKey,
        HeaderValueSourceType? PasswordSourceType,
        string? PasswordSourceKey,
        string? OAuthTokenEndpoint,
        HeaderValueSourceType? OAuthClientIdSourceType,
        string? OAuthClientIdSourceKey,
        HeaderValueSourceType? OAuthClientSecretSourceType,
        string? OAuthClientSecretSourceKey,
        string? OAuthScope);

    private sealed record AuthenticationValueResolutionResult(
        string? ResolvedValue,
        string? ErrorType,
        string? ErrorMessage);
}
