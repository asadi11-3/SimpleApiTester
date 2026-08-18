using MediatR;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Application.Operations.Commands.UpdateOperation;

public sealed record UpdateOperationCommand(
    Guid Id,
    Guid DataSourceId,
    string ApiName,
    string Endpoint,
    HttpMethodType MethodType,
    string? Body,
    string? ContentType,
    OperationAuthenticationMode AuthenticationMode) : IRequest;
