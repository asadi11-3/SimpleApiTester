using SimpleApiTester.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.Operations
{
    public sealed record OperationResponse(
    Guid Id,
    Guid DataSourceId,
    string ApiName,
    string Endpoint,
    HttpMethodType MethodType,
    string? Body,
    string? ContentType,
    OperationAuthenticationMode AuthenticationMode);
}
