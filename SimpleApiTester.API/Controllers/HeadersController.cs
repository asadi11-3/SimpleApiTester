using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.Headers;
using SimpleApiTester.Application.Headers.Commands.CreateDataSourceHeader;
using SimpleApiTester.Application.Headers.Commands.CreateOperationHeader;
using SimpleApiTester.Application.Headers.Commands.DeleteHeader;
using SimpleApiTester.Application.Headers.Commands.UpdateHeader;
using SimpleApiTester.Application.Headers.Queries.GetDataSourceHeaders;
using SimpleApiTester.Application.Headers.Queries.GetOperationHeaders;

namespace SimpleApiTester.API.Controllers;

[ApiController]
[Route("api")]
public sealed class HeadersController : ControllerBase
{
    private readonly ISender _sender;

    public HeadersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("data-sources/{dataSourceId:guid}/headers")]
    public async Task<IActionResult> CreateForDataSource(
        Guid dataSourceId,
        CreateHeaderRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new CreateDataSourceHeaderCommand(
                dataSourceId,
                request.Key,
                request.ValueSourceType,
                request.Value,
                request.SourceKey,
                request.IsEnabled),
            cancellationToken);

        return CreatedAtAction(nameof(GetByDataSource), new { dataSourceId }, new { id });
    }

    [HttpGet("data-sources/{dataSourceId:guid}/headers")]
    public async Task<IActionResult> GetByDataSource(Guid dataSourceId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDataSourceHeadersQuery(dataSourceId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("operations/{operationId:guid}/headers")]
    public async Task<IActionResult> CreateForOperation(
        Guid operationId,
        CreateHeaderRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new CreateOperationHeaderCommand(
                operationId,
                request.Key,
                request.ValueSourceType,
                request.Value,
                request.SourceKey,
                request.IsEnabled),
            cancellationToken);

        return CreatedAtAction(nameof(GetByOperation), new { operationId }, new { id });
    }

    [HttpGet("operations/{operationId:guid}/headers")]
    public async Task<IActionResult> GetByOperation(Guid operationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOperationHeadersQuery(operationId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("headers/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateHeaderRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new UpdateHeaderCommand(
                id,
                request.Key,
                request.ValueSourceType,
                request.Value,
                request.SourceKey,
                request.IsEnabled),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("headers/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteHeaderCommand(id), cancellationToken);
        return NoContent();
    }
}
