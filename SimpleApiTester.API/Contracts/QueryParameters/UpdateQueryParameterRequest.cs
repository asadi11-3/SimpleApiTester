namespace SimpleApiTester.API.Contracts.QueryParameters;

public sealed record UpdateQueryParameterRequest(
    Guid OperationId,
    string Key,
    string? Value,
    bool IsEnabled);
