using SimpleApiTester.Application.Operations.Commands.ExecuteOperation;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Abstractions.Http;

public interface IOperationRequestExecutor
{
    Task<ExecuteOperationResponse> ExecuteAsync(
        OperationHttpRequest request,
        CancellationToken cancellationToken);
}

public sealed record OperationHttpRequest(
    string Url,
    HttpMethodType MethodType,
    string? Body);
