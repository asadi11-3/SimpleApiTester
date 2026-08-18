namespace SimpleApiTester.API.Contracts.DataSourceEnvironments;

public sealed record CreateDataSourceEnvironmentRequest(
    string Name,
    string BaseUrl,
    bool IsActive);
