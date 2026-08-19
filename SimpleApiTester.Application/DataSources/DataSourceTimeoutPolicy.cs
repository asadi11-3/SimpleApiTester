namespace SimpleApiTester.Application.DataSources;

public static class DataSourceTimeoutPolicy
{
    public const int DefaultTimeoutSeconds = 100;
    public const int MinTimeoutSeconds = 1;
    public const int MaxTimeoutSeconds = 300;

    public static TimeSpan Resolve(int? configuredTimeoutSeconds)
    {
        var seconds = configuredTimeoutSeconds ?? DefaultTimeoutSeconds;
        return TimeSpan.FromSeconds(seconds);
    }
}
