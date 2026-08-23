using SimpleApiTester.Application.Headers;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.DataSourceAuthentications;

internal static class DataSourceAuthenticationRules
{
    public static bool HasCrOrLf(string value)
        => value.Contains('\r') || value.Contains('\n');

    public static bool IsAllowedValueSourceType(HeaderValueSourceType valueSourceType)
        => valueSourceType is HeaderValueSourceType.Variable
            or HeaderValueSourceType.UserSecret
            or HeaderValueSourceType.EnvironmentVariable;

    public static string? NormalizeSourceKey(string? sourceKey)
    {
        if (sourceKey is null)
        {
            return null;
        }

        var trimmed = sourceKey.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string? NormalizeApiKeyHeaderName(string? apiKeyHeaderName)
    {
        if (apiKeyHeaderName is null)
        {
            return null;
        }

        var trimmed = apiKeyHeaderName.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    public static ApiKeyLocation NormalizeApiKeyLocation(ApiKeyLocation? apiKeyLocation)
        => apiKeyLocation ?? ApiKeyLocation.Header;

    public static bool IsDisallowedApiKeyHeaderName(string headerName)
        => HeaderRules.IsReservedHeaderKey(headerName)
            || string.Equals(headerName.Trim(), "Authorization", StringComparison.OrdinalIgnoreCase);

    public static bool IsDisallowedApiKeyQueryKeyCharacter(char character)
        => character is '?' or '&' or '=';

    public static string? GetEffectiveHeaderName(
        AuthenticationType authenticationType,
        ApiKeyLocation? apiKeyLocation,
        string? apiKeyHeaderName)
        => authenticationType switch
        {
            AuthenticationType.Bearer or AuthenticationType.Basic => "Authorization",
            AuthenticationType.ApiKey when NormalizeApiKeyLocation(apiKeyLocation) == ApiKeyLocation.Header => apiKeyHeaderName!,
            _ => null
        };

    public static string GetEffectiveQueryParameterName(string apiKeyHeaderName)
        => apiKeyHeaderName;

    public static bool ConflictsWithHeader(
        AuthenticationType authenticationType,
        ApiKeyLocation? apiKeyLocation,
        string? apiKeyHeaderName,
        string headerKey)
    {
        var effectiveHeaderName = GetEffectiveHeaderName(authenticationType, apiKeyLocation, apiKeyHeaderName);

        return effectiveHeaderName is not null
            && string.Equals(effectiveHeaderName, headerKey, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ConflictsWithQueryParameter(
        AuthenticationType authenticationType,
        ApiKeyLocation? apiKeyLocation,
        string? apiKeyHeaderName,
        string queryParameterKey)
        => authenticationType == AuthenticationType.ApiKey
            && NormalizeApiKeyLocation(apiKeyLocation) == ApiKeyLocation.Query
            && string.Equals(GetEffectiveQueryParameterName(apiKeyHeaderName!), queryParameterKey, StringComparison.Ordinal);

    public static string CreateConflictMessage(
        AuthenticationType authenticationType,
        ApiKeyLocation? apiKeyLocation,
        string? apiKeyHeaderName)
        => $"Structured {GetAuthenticationDisplayName(authenticationType)} authentication conflicts with raw header '{GetEffectiveHeaderName(authenticationType, apiKeyLocation, apiKeyHeaderName)}'.";

    public static string CreateQueryConflictMessage(string queryParameterKey)
        => $"Structured API key authentication conflicts with raw query parameter '{queryParameterKey}'.";

    public static string CreateResolutionErrorMessage(
        AuthenticationType authenticationType,
        string sourceKey)
        => authenticationType switch
        {
            AuthenticationType.Bearer => $"Unable to resolve Bearer authentication source '{sourceKey}'.",
            AuthenticationType.ApiKey => $"Unable to resolve API key source '{sourceKey}'.",
            _ => $"Unable to resolve {GetAuthenticationDisplayName(authenticationType)} authentication source '{sourceKey}'."
        };

    public static string CreateBasicUsernameResolutionErrorMessage(string sourceKey)
        => $"Unable to use Basic authentication username source '{sourceKey}'.";

    public static string CreateBasicPasswordResolutionErrorMessage(string sourceKey)
        => $"Unable to resolve Basic authentication password source '{sourceKey}'.";

    public static string GetAuthenticationDisplayName(AuthenticationType authenticationType)
        => authenticationType switch
        {
            AuthenticationType.Bearer => "Bearer",
            AuthenticationType.ApiKey => "API key",
            AuthenticationType.Basic => "Basic",
            _ => "structured"
        };
}
