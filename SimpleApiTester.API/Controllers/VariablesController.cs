using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.Variables;
using SimpleApiTester.Application.Variables.Commands.CreateVariable;
using SimpleApiTester.Application.Variables.Commands.DeleteVariable;
using SimpleApiTester.Application.Variables.Commands.UpdateVariable;
using SimpleApiTester.Application.Variables.Queries.GetVariablesByDataSource;

namespace SimpleApiTester.API.Controllers;

[ApiController]
[Route("api")]
public sealed class VariablesController : ControllerBase
{
    private readonly ISender _sender;

    public VariablesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("data-sources/{dataSourceId:guid}/variables")]
    public async Task<IActionResult> Create(
        Guid dataSourceId,
        CreateVariableRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new CreateVariableCommand(dataSourceId, request.Key, request.Value, request.IsEnabled),
            cancellationToken);

        return CreatedAtAction(nameof(GetByDataSource), new { dataSourceId }, new { id });
    }

    [HttpGet("data-sources/{dataSourceId:guid}/variables")]
    public async Task<IActionResult> GetByDataSource(
        Guid dataSourceId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetVariablesByDataSourceQuery(dataSourceId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("variables/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateVariableRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new UpdateVariableCommand(id, request.Key, request.Value, request.IsEnabled),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("variables/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteVariableCommand(id), cancellationToken);
        return NoContent();
    }
}
