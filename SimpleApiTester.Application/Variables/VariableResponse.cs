namespace SimpleApiTester.Application.Variables;

public sealed record VariableResponse(
    Guid Id,
    Guid DataSourceEnvironmentId,
    string Key,
    string? Value,
    bool IsEnabled,
    bool IsSecret);
