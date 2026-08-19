using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.DataSources;
using SimpleApiTester.Application.DataSources;
using SimpleApiTester.Application.DataSources.Commands.CreateDataSource;
using SimpleApiTester.Application.DataSources.Commands.DeleteDataSource;
using SimpleApiTester.Application.DataSources.Commands.UpdateDataSource;
using SimpleApiTester.Application.DataSources.Queries.GetById;
using SimpleApiTester.Application.DataSources.Queries.GetDataSources;

namespace SimpleApiTester.API.Controllers
{
    [ApiController]
    [Route("api/data-sources")]
    public sealed class DataSourcesController : ControllerBase
    {
        private readonly ISender _sender;

        public DataSourcesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateDataSourceRequest request,
            CancellationToken cancellationToken)
        {
            var id = await _sender.Send(
                new CreateDataSourceCommand(request.Key, request.DefaultTimeoutSeconds),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new { id });
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<DataSourceResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyCollection<DataSourceResponse>>> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new GetDataSourcesQuery(),
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(DataSourceResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<DataSourceResponse>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new GetDataSourceByIdQuery(id),
                cancellationToken);

            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateDataSourceRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new UpdateDataSourceCommand(
                    id,
                    request.Key,
                    request.IsActive,
                    request.DefaultTimeoutSeconds),
                cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new DeleteDataSourceCommand(id),
                cancellationToken);

            return NoContent();
        }
    }
}
