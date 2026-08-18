namespace SimpleApiTester.Application.DataSourceEnvironments;

internal static class DataSourceEnvironmentRules
{
    public static string NormalizeBaseUrl(string baseUrl)
        => baseUrl.Trim().TrimEnd('/');

    public static bool IsValidBaseUrl(string baseUrl)
    {
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
