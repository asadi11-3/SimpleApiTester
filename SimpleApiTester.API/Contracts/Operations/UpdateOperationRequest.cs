using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.API.Contracts.Operations;

public sealed record UpdateOperationRequest(
    Guid DataSourceId,
    string ApiName,
    string Endpoint,
    HttpMethodType MethodType,
    string? Body,
    string? ContentType,
    OperationAuthenticationMode AuthenticationMode = OperationAuthenticationMode.Inherit);
