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

    public static string NormalizeSourceKey(string sourceKey)
        => sourceKey.Trim();

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
        => authenticationType == AuthenticationType.Bearer
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
        => authenticationType == AuthenticationType.Bearer
            ? $"Unable to resolve Bearer authentication source '{sourceKey}'."
            : $"Unable to resolve API key source '{sourceKey}'.";

    public static string GetAuthenticationDisplayName(AuthenticationType authenticationType)
        => authenticationType == AuthenticationType.Bearer ? "Bearer" : "API key";
}
