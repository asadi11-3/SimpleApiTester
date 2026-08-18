using MediatR;

namespace SimpleApiTester.Application.Headers.Commands.DeleteHeader;

public sealed record DeleteHeaderCommand(Guid Id) : IRequest;
