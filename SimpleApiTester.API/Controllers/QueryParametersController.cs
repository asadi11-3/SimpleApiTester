using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.QueryParameters;
using SimpleApiTester.Application.QueryParameters.Commands.CreateQueryParameter;
using SimpleApiTester.Application.QueryParameters.Commands.DeleteQueryParameter;
using SimpleApiTester.Application.QueryParameters.Commands.UpdateQueryParameter;
using SimpleApiTester.Application.QueryParameters.Queries.GetQueryParametersByOperation;

namespace SimpleApiTester.API.Controllers;

[ApiController]
[Route("api")]
public sealed class QueryParametersController : ControllerBase
{
    private readonly ISender _sender;

    public QueryParametersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("operations/{operationId:guid}/query-parameters")]
    public async Task<IActionResult> Create(
        Guid operationId,
        CreateQueryParameterRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new CreateQueryParameterCommand(
                operationId,
                request.Key,
                request.Value,
                request.IsEnabled),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetByOperation),
            new { operationId },
            new { id });
    }

    [HttpGet("operations/{operationId:guid}/query-parameters")]
    public async Task<IActionResult> GetByOperation(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetQueryParametersByOperationQuery(operationId),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("query-parameters/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateQueryParameterRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new UpdateQueryParameterCommand(
                id,
                request.OperationId,
                request.Key,
                request.Value,
                request.IsEnabled),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("query-parameters/{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteQueryParameterCommand(id), cancellationToken);

        return NoContent();
    }
}
