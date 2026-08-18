using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.API.Contracts.Headers;

public sealed record CreateHeaderRequest(
    string Key,
    HeaderValueSourceType ValueSourceType,
    string? Value,
    string? SourceKey,
    bool IsEnabled);
