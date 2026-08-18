using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.Variables;
using SimpleApiTester.Application.Variables.Commands.CreateVariable;
using SimpleApiTester.Application.Variables.Commands.DeleteVariable;
using SimpleApiTester.Application.Variables.Commands.UpdateVariable;
using SimpleApiTester.Application.Variables.Queries.GetVariablesByEnvironment;

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

    [HttpPost("environments/{environmentId:guid}/variables")]
    public async Task<IActionResult> Create(
        Guid environmentId,
        CreateVariableRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new CreateVariableCommand(environmentId, request.Key, request.Value, request.IsEnabled),
            cancellationToken);

        return CreatedAtAction(nameof(GetByEnvironment), new { environmentId }, new { id });
    }

    [HttpGet("environments/{environmentId:guid}/variables")]
    public async Task<IActionResult> GetByEnvironment(
        Guid environmentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetVariablesByEnvironmentQuery(environmentId), cancellationToken);
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
