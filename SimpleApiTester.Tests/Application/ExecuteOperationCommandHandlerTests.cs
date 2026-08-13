using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Operations.Commands.ExecuteOperation;
using SimpleApiTester.Infrastructure.Persistence;
using SimpleApiTester.Domain.Entities;
using SimpleApiTester.Domain.Enum;

namespace SimpleApiTester.Tests.Application;

public sealed class ExecuteOperationCommandHandlerTests
{
    [Fact]
    public async Task Handle_BuildsUrlWithQueryParameters_AndPassesBodyAndContentType()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        dbContext.DataSources.Add(new DataSource
        {
            Id = dataSourceId,
            Key = "jsonplaceholder",
            BaseUrl = "https://example.com",
            IsActive = true
        });

        dbContext.Operations.Add(new Operation
        {
            Id = operationId,
            DataSourceId = dataSourceId,
            ApiName = "Create post",
            Endpoint = "/posts",
            MethodType = HttpMethodType.Post,
            Body = "{\"title\":\"hello\"}",
            ContentType = "application/json"
        });

        dbContext.QueryParameters.AddRange(
            new QueryParameter
            {
                Id = Guid.NewGuid(),
                OperationId = operationId,
                Key = "userId",
                Value = "1",
                IsEnabled = true
            },
            new QueryParameter
            {
                Id = Guid.NewGuid(),
                OperationId = operationId,
                Key = "debug",
                Value = "true",
                IsEnabled = false
            });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = new ExecuteOperationCommandHandler(dbContext, executor);

        await handler.Handle(new ExecuteOperationCommand(operationId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        Assert.Equal("https://example.com/posts?userId=1", executor.Request!.Url);
        Assert.Equal(HttpMethodType.Post, executor.Request.MethodType);
        Assert.Equal("{\"title\":\"hello\"}", executor.Request.Body);
        Assert.Equal("application/json", executor.Request.ContentType);
    }

    [Fact]
    public async Task Handle_UsesNullContentType_WhenOperationDoesNotSpecifyOne()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        dbContext.DataSources.Add(new DataSource
        {
            Id = dataSourceId,
            Key = "jsonplaceholder",
            BaseUrl = "https://example.com",
            IsActive = true
        });

        dbContext.Operations.Add(new Operation
        {
            Id = operationId,
            DataSourceId = dataSourceId,
            ApiName = "Get posts",
            Endpoint = "/posts",
            MethodType = HttpMethodType.Get,
            Body = "{\"ignored\":true}",
            ContentType = null
        });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = new ExecuteOperationCommandHandler(dbContext, executor);

        await handler.Handle(new ExecuteOperationCommand(operationId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        Assert.Equal("https://example.com/posts", executor.Request!.Url);
        Assert.Null(executor.Request.ContentType);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class CapturingExecutor : IOperationRequestExecutor
    {
        public OperationHttpRequest? Request { get; private set; }

        public Task<ExecuteOperationResponse> ExecuteAsync(OperationHttpRequest request, CancellationToken cancellationToken)
        {
            Request = request;

            return Task.FromResult(new ExecuteOperationResponse(
                StatusCode: 200,
                IsSuccessStatusCode: true,
                ResponseBody: "ok",
                ContentType: "application/json",
                DurationMilliseconds: 1,
                HasExecutionError: false,
                ErrorType: null,
                ErrorMessage: null));
        }
    }
}
