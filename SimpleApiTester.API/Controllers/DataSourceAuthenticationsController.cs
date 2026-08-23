using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.DataSourceAuthentications;
using SimpleApiTester.Application.DataSourceAuthentications;
using SimpleApiTester.Application.DataSourceAuthentications.Commands.DeleteDataSourceAuthentication;
using SimpleApiTester.Application.DataSourceAuthentications.Commands.UpsertDataSourceAuthentication;
using SimpleApiTester.Application.DataSourceAuthentications.Queries.GetDataSourceAuthentication;

namespace SimpleApiTester.API.Controllers;

[ApiController]
[Route("api")]
public sealed class DataSourceAuthenticationsController : ControllerBase
{
    private readonly ISender _sender;

    public DataSourceAuthenticationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("data-sources/{dataSourceId:guid}/authentication")]
    [ProducesResponseType(typeof(DataSourceAuthenticationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DataSourceAuthenticationResponse>> Get(
        Guid dataSourceId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDataSourceAuthenticationQuery(dataSourceId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("data-sources/{dataSourceId:guid}/authentication")]
    public async Task<IActionResult> Upsert(
        Guid dataSourceId,
        UpsertDataSourceAuthenticationRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new UpsertDataSourceAuthenticationCommand(
                dataSourceId,
                request.AuthenticationType,
                request.ValueSourceType,
                request.SourceKey,
                request.ApiKeyHeaderName,
                request.UsernameSourceType,
                request.UsernameSourceKey,
                request.PasswordSourceType,
                request.PasswordSourceKey),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("data-sources/{dataSourceId:guid}/authentication")]
    public async Task<IActionResult> Delete(
        Guid dataSourceId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteDataSourceAuthenticationCommand(dataSourceId), cancellationToken);
        return NoContent();
    }
}
