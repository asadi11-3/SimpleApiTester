namespace SimpleApiTester.Application.Variables;

public sealed record VariableResponse(
    Guid Id,
    Guid DataSourceId,
    string Key,
    string? Value,
    bool IsEnabled);
