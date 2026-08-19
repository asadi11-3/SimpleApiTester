using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.DataSources;
using SimpleApiTester.Application.DataSources.Commands.TestConnection;
using SimpleApiTester.Domain.Entities;
using SimpleApiTester.Infrastructure.Persistence;

namespace SimpleApiTester.Tests.Application;

public sealed class TestDataSourceConnectionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenDataSourceDoesNotExist_ThrowsNotFoundAndDoesNotInvokeTester()
    {
        await using var dbContext = CreateDbContext();
        var tester = new CapturingConnectionTester();
        var handler = CreateHandler(dbContext, tester);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new TestDataSourceConnectionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("DataSource not found.", exception.Message);
        Assert.Null(tester.BaseUrl);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentDoesNotExist_ThrowsNotFoundAndDoesNotInvokeTester()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        SeedDataSource(dbContext, dataSourceId);
        await dbContext.SaveChangesAsync();

        var tester = new CapturingConnectionTester();
        var handler = CreateHandler(dbContext, tester);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new TestDataSourceConnectionCommand(dataSourceId, Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("Environment not found.", exception.Message);
        Assert.Null(tester.BaseUrl);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentBelongsToAnotherDataSource_ThrowsNotFoundAndDoesNotInvokeTester()
    {
        await using var dbContext = CreateDbContext();

        var firstDataSourceId = Guid.NewGuid();
        var secondDataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        SeedDataSource(dbContext, firstDataSourceId, key: "first");
        SeedDataSource(dbContext, secondDataSourceId, key: "second");
        SeedEnvironment(dbContext, environmentId, secondDataSourceId, baseUrl: "https://other.example.com");
        await dbContext.SaveChangesAsync();

        var tester = new CapturingConnectionTester();
        var handler = CreateHandler(dbContext, tester);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new TestDataSourceConnectionCommand(firstDataSourceId, environmentId), CancellationToken.None));

        Assert.Equal("Environment not found.", exception.Message);
        Assert.Null(tester.BaseUrl);
    }

    [Fact]
    public async Task Handle_WhenDataSourceIsInactive_ThrowsConflictAndDoesNotInvokeTester()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId, isActive: false);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        await dbContext.SaveChangesAsync();

        var tester = new CapturingConnectionTester();
        var handler = CreateHandler(dbContext, tester);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new TestDataSourceConnectionCommand(dataSourceId, environmentId), CancellationToken.None));

        Assert.Equal("Cannot execute an operation for an inactive data source.", exception.Message);
        Assert.Null(tester.BaseUrl);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentIsInactive_ThrowsConflictAndDoesNotInvokeTester()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com", isActive: false);
        await dbContext.SaveChangesAsync();

        var tester = new CapturingConnectionTester();
        var handler = CreateHandler(dbContext, tester);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new TestDataSourceConnectionCommand(dataSourceId, environmentId), CancellationToken.None));

        Assert.Equal("Cannot execute an operation for an inactive environment.", exception.Message);
        Assert.Null(tester.BaseUrl);
    }

    [Fact]
    public async Task Handle_PassesExactEnvironmentBaseUrl_ToConnectionTester()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://dev.example.com/root");
        await dbContext.SaveChangesAsync();

        var expected = new TestDataSourceConnectionResponse(true, 404, false, 73, "application/json", null, null);
        var tester = new CapturingConnectionTester(expected);
        var handler = CreateHandler(dbContext, tester);

        var response = await handler.Handle(new TestDataSourceConnectionCommand(dataSourceId, environmentId), CancellationToken.None);

        Assert.Equal("https://dev.example.com/root", tester.BaseUrl);
        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Handle_DoesNotAlterSafeTransportErrors()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://dev.example.com/root");
        await dbContext.SaveChangesAsync();

        var tester = new CapturingConnectionTester(
            new TestDataSourceConnectionResponse(false, null, null, 25, null, "HttpRequestError", "Unable to connect to target."));
        var handler = CreateHandler(dbContext, tester);

        var response = await handler.Handle(new TestDataSourceConnectionCommand(dataSourceId, environmentId), CancellationToken.None);

        Assert.False(response.IsReachable);
        Assert.Equal("HttpRequestError", response.ErrorType);
        Assert.Equal("Unable to connect to target.", response.ErrorMessage);
        Assert.DoesNotContain("Bearer", response.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ApiKey", response.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    private static TestDataSourceConnectionCommandHandler CreateHandler(AppDbContext dbContext, CapturingConnectionTester tester)
        => new(dbContext, tester);

    private static void SeedDataSource(AppDbContext dbContext, Guid dataSourceId, string key = "jsonplaceholder", bool isActive = true)
    {
        dbContext.DataSources.Add(new DataSource
        {
            Id = dataSourceId,
            Key = key,
            IsActive = isActive
        });
    }

    private static void SeedEnvironment(
        AppDbContext dbContext,
        Guid environmentId,
        Guid dataSourceId,
        string name = "Development",
        string baseUrl = "https://example.com",
        bool isActive = true)
    {
        dbContext.DataSourceEnvironments.Add(new DataSourceEnvironment
        {
            Id = environmentId,
            DataSourceId = dataSourceId,
            Name = name,
            BaseUrl = baseUrl,
            IsActive = isActive
        });
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class CapturingConnectionTester : IDataSourceConnectionTester
    {
        private readonly TestDataSourceConnectionResponse _response;

        public CapturingConnectionTester(TestDataSourceConnectionResponse? response = null)
        {
            _response = response ?? new TestDataSourceConnectionResponse(true, 200, true, 5, "text/plain", null, null);
        }

        public string? BaseUrl { get; private set; }

        public Task<TestDataSourceConnectionResponse> TestConnectionAsync(string baseUrl, CancellationToken cancellationToken)
        {
            BaseUrl = baseUrl;
            return Task.FromResult(_response);
        }
    }
}
