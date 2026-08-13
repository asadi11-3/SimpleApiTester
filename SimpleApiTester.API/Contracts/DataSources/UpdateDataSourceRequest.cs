namespace SimpleApiTester.API.Contracts.DataSources;

public sealed record UpdateDataSourceRequest(
    string Key,
    string BaseUrl,
    bool IsActive);
