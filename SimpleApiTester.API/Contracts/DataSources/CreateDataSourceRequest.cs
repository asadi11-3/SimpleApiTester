namespace SimpleApiTester.API.Contracts.DataSources;

public sealed record CreateDataSourceRequest(
    string Key,
    string BaseUrl);
