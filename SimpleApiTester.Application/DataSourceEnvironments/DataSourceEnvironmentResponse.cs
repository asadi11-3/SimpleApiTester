namespace SimpleApiTester.Application.DataSourceEnvironments;

public sealed record DataSourceEnvironmentResponse(
    Guid Id,
    Guid DataSourceId,
    string Name,
    string BaseUrl,
    bool IsActive);
