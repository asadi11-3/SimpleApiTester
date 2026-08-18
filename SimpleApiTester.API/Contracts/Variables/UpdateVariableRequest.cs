namespace SimpleApiTester.API.Contracts.Variables;

public sealed record UpdateVariableRequest(
    string Key,
    string? Value,
    bool IsEnabled,
    bool IsSecret);
