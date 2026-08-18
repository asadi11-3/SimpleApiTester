using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.API.Contracts.Headers;

public sealed record UpdateHeaderRequest(
    string Key,
    HeaderValueSourceType ValueSourceType,
    string? Value,
    string? SourceKey,
    bool IsEnabled);
