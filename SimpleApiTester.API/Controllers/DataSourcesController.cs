using MediatR;
using Microsoft.AspNetCore.Mvc;
using SimpleApiTester.API.Contracts.DataSources;
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
                new CreateDataSourceCommand(request.Key, request.BaseUrl),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new GetDataSourcesQuery(),
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
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
                    request.BaseUrl,
                    request.IsActive),
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
