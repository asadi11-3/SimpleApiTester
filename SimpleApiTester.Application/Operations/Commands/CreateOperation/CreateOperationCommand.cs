using MediatR;
using SimpleApiTester.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Application.Operations.Commands.CreateOperation
{
    public sealed record CreateOperationCommand(
    Guid DataSourceId,
    string ApiName,
    string Endpoint,
    HttpMethodType MethodType,
    string? Body,
    string? ContentType
) : IRequest<Guid>;
}
