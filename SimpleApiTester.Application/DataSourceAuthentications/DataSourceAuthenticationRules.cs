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

    public static bool IsDisallowedApiKeyHeaderName(string headerName)
        => HeaderRules.IsReservedHeaderKey(headerName)
            || string.Equals(headerName.Trim(), "Authorization", StringComparison.OrdinalIgnoreCase);

    public static string GetEffectiveHeaderName(AuthenticationType authenticationType, string? apiKeyHeaderName)
        => authenticationType is AuthenticationType.Bearer or AuthenticationType.Basic
            ? "Authorization"
            : apiKeyHeaderName!;

    public static bool ConflictsWithHeader(
        AuthenticationType authenticationType,
        string? apiKeyHeaderName,
        string headerKey)
        => string.Equals(
            GetEffectiveHeaderName(authenticationType, apiKeyHeaderName),
            headerKey,
            StringComparison.OrdinalIgnoreCase);

    public static string CreateConflictMessage(
        AuthenticationType authenticationType,
        string? apiKeyHeaderName)
        => $"Structured {GetAuthenticationDisplayName(authenticationType)} authentication conflicts with raw header '{GetEffectiveHeaderName(authenticationType, apiKeyHeaderName)}'.";

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
