using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SimpleApiTester.Application.DataSources;
using SimpleApiTester.Application.DataSources.Commands.TestConnection;

namespace SimpleApiTester.API.Controllers;

[ApiController]
[Route("api")]
public sealed class DataSourceConnectionsController : ControllerBase
{
    private readonly ISender _sender;

    public DataSourceConnectionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("data-sources/{dataSourceId:guid}/test-connection")]
    [ProducesResponseType(typeof(TestDataSourceConnectionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestDataSourceConnectionResponse>> TestConnection(
        Guid dataSourceId,
        [FromQuery, BindRequired] Guid environmentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new TestDataSourceConnectionCommand(dataSourceId, environmentId),
            cancellationToken);

        return Ok(result);
    }
    
}
