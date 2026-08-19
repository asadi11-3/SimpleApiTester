using SimpleApiTester.Application.Operations.Commands.ExecuteOperation;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Abstractions.Http;

public interface IOperationRequestExecutor
{
    Task<ExecuteOperationResponse> ExecuteAsync(
        OperationHttpRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public sealed record OperationHttpRequest(
    string Url,
    HttpMethodType MethodType,
    string? Body,
    string? ContentType,
    IReadOnlyCollection<ResolvedRequestHeader> Headers)
{
    public OperationHttpRequest(
        string url,
        HttpMethodType methodType,
        string? body,
        string? contentType)
        : this(url, methodType, body, contentType, Array.Empty<ResolvedRequestHeader>())
    {
    }
}

public sealed record ResolvedRequestHeader(string Key, string Value);
