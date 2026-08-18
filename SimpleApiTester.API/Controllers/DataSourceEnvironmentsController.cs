using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.DataSourceEnvironments;
using SimpleApiTester.Application.DataSourceEnvironments.Commands.CreateDataSourceEnvironment;
using SimpleApiTester.Application.DataSourceEnvironments.Commands.DeleteDataSourceEnvironment;
using SimpleApiTester.Application.DataSourceEnvironments.Commands.UpdateDataSourceEnvironment;
using SimpleApiTester.Application.DataSourceEnvironments.Queries.GetDataSourceEnvironmentById;
using SimpleApiTester.Application.DataSourceEnvironments.Queries.GetDataSourceEnvironments;

namespace SimpleApiTester.API.Controllers;

[ApiController]
[Route("api")]
public sealed class DataSourceEnvironmentsController : ControllerBase
{
    private readonly ISender _sender;

    public DataSourceEnvironmentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("data-sources/{dataSourceId:guid}/environments")]
    public async Task<IActionResult> Create(
        Guid dataSourceId,
        CreateDataSourceEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new CreateDataSourceEnvironmentCommand(dataSourceId, request.Name, request.BaseUrl, request.IsActive),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("data-sources/{dataSourceId:guid}/environments")]
    public async Task<IActionResult> GetByDataSource(
        Guid dataSourceId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDataSourceEnvironmentsQuery(dataSourceId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("environments/{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDataSourceEnvironmentByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("environments/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateDataSourceEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new UpdateDataSourceEnvironmentCommand(id, request.Name, request.BaseUrl, request.IsActive),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("environments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteDataSourceEnvironmentCommand(id), cancellationToken);
        return NoContent();
    }
}
