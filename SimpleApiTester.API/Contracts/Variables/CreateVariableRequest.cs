namespace SimpleApiTester.API.Contracts.Variables;

public sealed record CreateVariableRequest(
    string Key,
    string? Value,
    bool IsEnabled,
    bool IsSecret);
