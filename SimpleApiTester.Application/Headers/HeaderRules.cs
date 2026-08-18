using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Headers;

internal static class HeaderRules
{
    private static readonly HashSet<string> ReservedHeaderKeys =
    [
        "Content-Type",
        "Content-Length",
        "Host",
        "Transfer-Encoding"
    ];

    public static string NormalizeKey(string key) => key.Trim();

    public static string? NormalizeSourceKey(string? sourceKey)
    {
        if (sourceKey is null)
        {
            return null;
        }

        var trimmed = sourceKey.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    public static bool HasCrOrLf(string value)
        => value.Contains('\r') || value.Contains('\n');

    public static bool IsReservedHeaderKey(string key)
        => ReservedHeaderKeys.Contains(key.Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool HasValidValueShape(HeaderValueSourceType valueSourceType, string? value, string? sourceKey)
        => valueSourceType switch
        {
            HeaderValueSourceType.General => value is not null && string.IsNullOrWhiteSpace(sourceKey),
            HeaderValueSourceType.Variable => value is null && !string.IsNullOrWhiteSpace(sourceKey),
            HeaderValueSourceType.UserSecret => value is null && !string.IsNullOrWhiteSpace(sourceKey),
            HeaderValueSourceType.EnvironmentVariable => value is null && !string.IsNullOrWhiteSpace(sourceKey),
            _ => false
        };
}
