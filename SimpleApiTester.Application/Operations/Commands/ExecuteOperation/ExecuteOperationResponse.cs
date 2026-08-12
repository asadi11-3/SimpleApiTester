namespace SimpleApiTester.Application.Operations.Commands.ExecuteOperation;

public sealed record ExecuteOperationResponse(
    int? StatusCode,
    bool? IsSuccessStatusCode,
    string? ResponseBody,
    string? ContentType,
    long DurationMilliseconds,
    bool HasExecutionError,
    string? ErrorType,
    string? ErrorMessage);
