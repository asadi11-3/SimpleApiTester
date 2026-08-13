namespace SimpleApiTester.Application.QueryParameters;

public sealed record QueryParameterResponse(
    Guid Id,
    Guid OperationId,
    string Key,
    string? Value,
    bool IsEnabled);
