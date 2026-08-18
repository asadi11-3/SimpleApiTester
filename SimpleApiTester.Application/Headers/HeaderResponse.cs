using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Headers;

public sealed record HeaderResponse(
    Guid Id,
    Guid? DataSourceId,
    Guid? OperationId,
    string Key,
    string? Value,
    HeaderValueSourceType ValueSourceType,
    string? SourceKey,
    bool IsEnabled);
