using MediatR;

namespace SimpleApiTester.Application.QueryParameters.Commands.DeleteQueryParameter;

public sealed record DeleteQueryParameterCommand(Guid Id) : IRequest;
