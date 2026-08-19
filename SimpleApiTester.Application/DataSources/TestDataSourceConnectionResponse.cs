namespace SimpleApiTester.Application.DataSources;

public sealed record TestDataSourceConnectionResponse(
    bool IsReachable,
    int? StatusCode,
    bool? IsSuccessStatusCode,
    long DurationMilliseconds,
    string? ContentType,
    string? ErrorType,
    string? ErrorMessage);
