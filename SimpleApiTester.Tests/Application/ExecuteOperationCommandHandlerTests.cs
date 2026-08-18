using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Headers;
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
        var handler = new ExecuteOperationCommandHandler(dbContext, new StubExternalHeaderValueResolver(), executor);

        await handler.Handle(new ExecuteOperationCommand(operationId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        Assert.Equal("https://example.com/posts?userId=1", executor.Request!.Url);
        Assert.Equal(HttpMethodType.Post, executor.Request.MethodType);
        Assert.Equal("{\"title\":\"hello\"}", executor.Request.Body);
        Assert.Equal("application/json", executor.Request.ContentType);
    }

    [Fact]
    public async Task Handle_MergesEnabledHeaders_AndOperationOverridesDataSourceCaseInsensitively()
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
            MethodType = HttpMethodType.Get
        });

        dbContext.Headers.AddRange(
            new Header
            {
                Id = Guid.NewGuid(),
                DataSourceId = dataSourceId,
                Key = "Authorization",
                ValueSourceType = HeaderValueSourceType.General,
                Value = "Bearer data-source",
                IsEnabled = true
            },
            new Header
            {
                Id = Guid.NewGuid(),
                DataSourceId = dataSourceId,
                Key = "X-Trace",
                ValueSourceType = HeaderValueSourceType.General,
                Value = "trace-1",
                IsEnabled = true
            },
            new Header
            {
                Id = Guid.NewGuid(),
                OperationId = operationId,
                Key = "authorization",
                ValueSourceType = HeaderValueSourceType.General,
                Value = "Bearer operation",
                IsEnabled = true
            });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = new ExecuteOperationCommandHandler(dbContext, new StubExternalHeaderValueResolver(), executor);

        await handler.Handle(new ExecuteOperationCommand(operationId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        Assert.Collection(
            executor.Request!.Headers.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase),
            header =>
            {
                Assert.Equal("authorization", header.Key, ignoreCase: true);
                Assert.Equal("Bearer operation", header.Value);
            },
            header =>
            {
                Assert.Equal("X-Trace", header.Key, ignoreCase: true);
                Assert.Equal("trace-1", header.Value);
            });
    }

    [Fact]
    public async Task Handle_DisabledOperationHeader_DoesNotOverrideEnabledDataSourceHeader()
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
            MethodType = HttpMethodType.Get
        });

        dbContext.Headers.AddRange(
            new Header
            {
                Id = Guid.NewGuid(),
                DataSourceId = dataSourceId,
                Key = "Authorization",
                ValueSourceType = HeaderValueSourceType.General,
                Value = "Bearer data-source",
                IsEnabled = true
            },
            new Header
            {
                Id = Guid.NewGuid(),
                OperationId = operationId,
                Key = "Authorization",
                ValueSourceType = HeaderValueSourceType.General,
                Value = "Bearer operation",
                IsEnabled = false
            });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = new ExecuteOperationCommandHandler(dbContext, new StubExternalHeaderValueResolver(), executor);

        await handler.Handle(new ExecuteOperationCommand(operationId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        var singleHeader = Assert.Single(executor.Request!.Headers);
        Assert.Equal("Bearer data-source", singleHeader.Value);
    }

    [Fact]
    public async Task Handle_ResolvesVariableAndExternalHeaderSources()
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
            MethodType = HttpMethodType.Get
        });

        dbContext.Variables.Add(new Variable
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            Key = "ApiToken",
            Value = "token-123",
            IsEnabled = true
        });

        dbContext.Headers.AddRange(
            new Header
            {
                Id = Guid.NewGuid(),
                DataSourceId = dataSourceId,
                Key = "X-Variable",
                ValueSourceType = HeaderValueSourceType.Variable,
                SourceKey = "apitoken",
                IsEnabled = true
            },
            new Header
            {
                Id = Guid.NewGuid(),
                DataSourceId = dataSourceId,
                Key = "X-Secret",
                ValueSourceType = HeaderValueSourceType.UserSecret,
                SourceKey = "Secrets:ApiKey",
                IsEnabled = true
            },
            new Header
            {
                Id = Guid.NewGuid(),
                DataSourceId = dataSourceId,
                Key = "X-Environment",
                ValueSourceType = HeaderValueSourceType.EnvironmentVariable,
                SourceKey = "SIMPLE_API_TESTER_TOKEN",
                IsEnabled = true
            });

        await dbContext.SaveChangesAsync();

        var resolver = new StubExternalHeaderValueResolver(
            userSecrets: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Secrets:ApiKey"] = "secret-456"
            },
            environmentVariables: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SIMPLE_API_TESTER_TOKEN"] = "env-789"
            });

        var executor = new CapturingExecutor();
        var handler = new ExecuteOperationCommandHandler(dbContext, resolver, executor);

        await handler.Handle(new ExecuteOperationCommand(operationId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        Assert.Equal(3, executor.Request!.Headers.Count);
        Assert.Contains(executor.Request.Headers, x => x.Key == "X-Variable" && x.Value == "token-123");
        Assert.Contains(executor.Request.Headers, x => x.Key == "X-Secret" && x.Value == "secret-456");
        Assert.Contains(executor.Request.Headers, x => x.Key == "X-Environment" && x.Value == "env-789");
    }

    [Fact]
    public async Task Handle_WhenHeaderVariableCannotResolve_ReturnsExecutionErrorAndSkipsExecutor()
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
            MethodType = HttpMethodType.Get
        });

        dbContext.Variables.Add(new Variable
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            Key = "ApiToken",
            Value = "token-123",
            IsEnabled = false
        });

        dbContext.Headers.Add(new Header
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            Key = "X-Variable",
            ValueSourceType = HeaderValueSourceType.Variable,
            SourceKey = "ApiToken",
            IsEnabled = true
        });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = new ExecuteOperationCommandHandler(dbContext, new StubExternalHeaderValueResolver(), executor);

        var response = await handler.Handle(new ExecuteOperationCommand(operationId), CancellationToken.None);

        Assert.True(response.HasExecutionError);
        Assert.Equal("HeaderResolutionError", response.ErrorType);
        Assert.Contains("ApiToken", response.ErrorMessage);
        Assert.Null(executor.Request);
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
        var handler = new ExecuteOperationCommandHandler(dbContext, new StubExternalHeaderValueResolver(), executor);

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

    private sealed class StubExternalHeaderValueResolver : IExternalHeaderValueResolver
    {
        private readonly IReadOnlyDictionary<string, string?> _environmentVariables;
        private readonly IReadOnlyDictionary<string, string?> _userSecrets;

        public StubExternalHeaderValueResolver(
            IReadOnlyDictionary<string, string?>? userSecrets = null,
            IReadOnlyDictionary<string, string?>? environmentVariables = null)
        {
            _userSecrets = userSecrets ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            _environmentVariables = environmentVariables ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        public Task<string?> ResolveAsync(Domain.Enum.HeaderValueSourceType valueSourceType, string sourceKey, CancellationToken cancellationToken)
        {
            var value = valueSourceType switch
            {
                HeaderValueSourceType.UserSecret => _userSecrets.TryGetValue(sourceKey, out var userSecretValue)
                    ? userSecretValue
                    : null,
                HeaderValueSourceType.EnvironmentVariable => _environmentVariables.TryGetValue(sourceKey, out var environmentValue)
                    ? environmentValue
                    : null,
                _ => null
            };

            return Task.FromResult(value);
        }
    }
}
