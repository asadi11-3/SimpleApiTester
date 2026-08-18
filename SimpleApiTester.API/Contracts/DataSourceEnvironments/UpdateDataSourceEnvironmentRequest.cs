namespace SimpleApiTester.API.Contracts.DataSourceEnvironments;

public sealed record UpdateDataSourceEnvironmentRequest(
    string Name,
    string BaseUrl,
    bool IsActive);
