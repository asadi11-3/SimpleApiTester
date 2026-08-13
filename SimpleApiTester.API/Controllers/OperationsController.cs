using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.Operations;
using SimpleApiTester.Application.Operations.Commands.CreateOperation;
using SimpleApiTester.Application.Operations.Commands.DeleteOperation;
using SimpleApiTester.Application.Operations.Commands.ExecuteOperation;
using SimpleApiTester.Application.Operations.Commands.UpdateOperation;
using SimpleApiTester.Application.Operations.Queries.GetOperationById;
using SimpleApiTester.Application.Operations.Queries.GetOperationsByDataSource;

namespace SimpleApiTester.API.Controllers;

[ApiController]
[Route("api")]
public sealed class OperationsController : ControllerBase
{
    private readonly ISender _sender;

    public OperationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("data-sources/{dataSourceId:guid}/operations")]
    public async Task<IActionResult> Create(
        Guid dataSourceId,
        CreateOperationRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new CreateOperationCommand(
                dataSourceId,
                request.ApiName,
                request.Endpoint,
                request.MethodType,
                request.Body,
                request.ContentType),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id });
    }

    [HttpGet("data-sources/{dataSourceId:guid}/operations")]
    public async Task<IActionResult> GetByDataSource(
        Guid dataSourceId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetOperationsByDataSourceQuery(dataSourceId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("operations/{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetOperationByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("operations/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateOperationRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new UpdateOperationCommand(
                id,
                request.DataSourceId,
                request.ApiName,
                request.Endpoint,
                request.MethodType,
                request.Body,
                request.ContentType),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("operations/{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteOperationCommand(id), cancellationToken);

        return NoContent();
    }

    [HttpPost("operations/{id:guid}/execute")]
    public async Task<IActionResult> Execute(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ExecuteOperationCommand(id),
            cancellationToken);

        return Ok(result);
    }
}
