using System.Text.RegularExpressions;

namespace SimpleApiTester.Application.Operations;

internal static partial class ContentTypeRules
{
    private const int MaxLength = 200;

    [GeneratedRegex(@"^[!#$%&'*+.^_`|~0-9A-Za-z-]+/[!#$%&'*+.^_`|~0-9A-Za-z-]+(?:\s*;\s*[!#$%&'*+.^_`|~0-9A-Za-z-]+=(?:[!#$%&'*+.^_`|~0-9A-Za-z-]+|\""[^\""\r\n]*\""))*$", RegexOptions.CultureInvariant)]
    private static partial Regex MediaTypeRegex();

    public static bool IsValid(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        var normalizedContentType = Normalize(contentType);

        if (normalizedContentType is null)
        {
            return true;
        }

        if (normalizedContentType.Length > MaxLength)
        {
            return false;
        }

        if (normalizedContentType.Contains('\r') || normalizedContentType.Contains('\n'))
        {
            return false;
        }

        return MediaTypeRegex().IsMatch(normalizedContentType);
    }

    public static string? Normalize(string? contentType)
    {
        if (contentType is null)
        {
            return null;
        }

        var trimmedContentType = contentType.Trim();

        return trimmedContentType.Length == 0
            ? null
            : trimmedContentType;
    }
}
