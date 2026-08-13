namespace SimpleApiTester.API.Contracts.QueryParameters;

public sealed record CreateQueryParameterRequest(
    string Key,
    string? Value,
    bool IsEnabled);
