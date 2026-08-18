using Microsoft.EntityFrameworkCore;
using SimpleApiTester.Application.Abstractions.Headers;
using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Operations.Commands.ExecuteOperation;
using SimpleApiTester.Domain.Entities;
using SimpleApiTester.Domain.Enum;
using SimpleApiTester.Infrastructure.Persistence;

namespace SimpleApiTester.Tests.Application;

public sealed class ExecuteOperationCommandHandlerTests
{
    [Fact]
    public async Task Handle_BuildsUrlWithQueryParameters_AndPassesBodyAndContentType()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        SeedOperation(dbContext, operationId, dataSourceId, HttpMethodType.Post, body: "{\"title\":\"hello\"}", contentType: "application/json");

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
        var handler = CreateHandler(dbContext, executor);

        await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        Assert.Equal("https://example.com/posts?userId=1", executor.Request!.Url);
        Assert.Equal(HttpMethodType.Post, executor.Request.MethodType);
        Assert.Equal("{\"title\":\"hello\"}", executor.Request.Body);
        Assert.Equal("application/json", executor.Request.ContentType);
    }

    [Fact]
    public async Task Handle_UsesSelectedEnvironmentBaseUrl_AndVariableValues()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var developmentEnvironmentId = Guid.NewGuid();
        var productionEnvironmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, developmentEnvironmentId, dataSourceId, name: "Development", baseUrl: "https://dev.example.com");
        SeedEnvironment(dbContext, productionEnvironmentId, dataSourceId, name: "Production", baseUrl: "https://prod.example.com");
        SeedOperation(dbContext, operationId, dataSourceId);

        dbContext.Variables.AddRange(
            new Variable
            {
                Id = Guid.NewGuid(),
                DataSourceEnvironmentId = developmentEnvironmentId,
                Key = "ApiToken",
                Value = "dev-token",
                IsEnabled = true
            },
            new Variable
            {
                Id = Guid.NewGuid(),
                DataSourceEnvironmentId = productionEnvironmentId,
                Key = "ApiToken",
                Value = "prod-token",
                IsEnabled = true
            });

        dbContext.Headers.Add(new Header
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            Key = "Authorization",
            ValueSourceType = HeaderValueSourceType.Variable,
            SourceKey = "ApiToken",
            IsEnabled = true
        });

        await dbContext.SaveChangesAsync();

        var developmentExecutor = new CapturingExecutor();
        var developmentHandler = CreateHandler(dbContext, developmentExecutor);

        await developmentHandler.Handle(new ExecuteOperationCommand(operationId, developmentEnvironmentId), CancellationToken.None);

        Assert.NotNull(developmentExecutor.Request);
        Assert.Equal("https://dev.example.com/posts", developmentExecutor.Request!.Url);
        Assert.Contains(developmentExecutor.Request.Headers, x => x.Key == "Authorization" && x.Value == "dev-token");

        var productionExecutor = new CapturingExecutor();
        var productionHandler = CreateHandler(dbContext, productionExecutor);

        await productionHandler.Handle(new ExecuteOperationCommand(operationId, productionEnvironmentId), CancellationToken.None);

        Assert.NotNull(productionExecutor.Request);
        Assert.Equal("https://prod.example.com/posts", productionExecutor.Request!.Url);
        Assert.Contains(productionExecutor.Request.Headers, x => x.Key == "Authorization" && x.Value == "prod-token");
    }

    [Fact]
    public async Task Handle_MergesEnabledHeaders_AndOperationOverridesDataSourceCaseInsensitively()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        SeedOperation(dbContext, operationId, dataSourceId);

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
        var handler = CreateHandler(dbContext, executor);

        await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

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
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        SeedOperation(dbContext, operationId, dataSourceId);

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
        var handler = CreateHandler(dbContext, executor);

        await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        var singleHeader = Assert.Single(executor.Request!.Headers);
        Assert.Equal("Bearer data-source", singleHeader.Value);
    }

    [Fact]
    public async Task Handle_ResolvesVariableAndExternalHeaderSources_FromSelectedEnvironmentOnly()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var developmentEnvironmentId = Guid.NewGuid();
        var stagingEnvironmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, developmentEnvironmentId, dataSourceId, name: "Development", baseUrl: "https://example.com");
        SeedEnvironment(dbContext, stagingEnvironmentId, dataSourceId, name: "Staging", baseUrl: "https://staging.example.com");
        SeedOperation(dbContext, operationId, dataSourceId);

        dbContext.Variables.AddRange(
            new Variable
            {
                Id = Guid.NewGuid(),
                DataSourceEnvironmentId = developmentEnvironmentId,
                Key = "ApiToken",
                Value = "token-123",
                IsEnabled = true
            },
            new Variable
            {
                Id = Guid.NewGuid(),
                DataSourceEnvironmentId = stagingEnvironmentId,
                Key = "ApiToken",
                Value = "wrong-token",
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

        await handler.Handle(new ExecuteOperationCommand(operationId, developmentEnvironmentId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        Assert.Equal(3, executor.Request!.Headers.Count);
        Assert.Contains(executor.Request.Headers, x => x.Key == "X-Variable" && x.Value == "token-123");
        Assert.DoesNotContain(executor.Request.Headers, x => x.Key == "X-Variable" && x.Value == "wrong-token");
        Assert.Contains(executor.Request.Headers, x => x.Key == "X-Secret" && x.Value == "secret-456");
        Assert.Contains(executor.Request.Headers, x => x.Key == "X-Environment" && x.Value == "env-789");
    }

    [Fact]
    public async Task Handle_WhenHeaderVariableCannotResolveInSelectedEnvironment_ReturnsExecutionErrorAndSkipsExecutor()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var selectedEnvironmentId = Guid.NewGuid();
        var otherEnvironmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, selectedEnvironmentId, dataSourceId, name: "Development", baseUrl: "https://example.com");
        SeedEnvironment(dbContext, otherEnvironmentId, dataSourceId, name: "Production", baseUrl: "https://prod.example.com");
        SeedOperation(dbContext, operationId, dataSourceId);

        dbContext.Variables.AddRange(
            new Variable
            {
                Id = Guid.NewGuid(),
                DataSourceEnvironmentId = selectedEnvironmentId,
                Key = "ApiToken",
                Value = "token-123",
                IsEnabled = false
            },
            new Variable
            {
                Id = Guid.NewGuid(),
                DataSourceEnvironmentId = otherEnvironmentId,
                Key = "ApiToken",
                Value = "other-token",
                IsEnabled = true
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
        var handler = CreateHandler(dbContext, executor);

        var response = await handler.Handle(new ExecuteOperationCommand(operationId, selectedEnvironmentId), CancellationToken.None);

        Assert.True(response.HasExecutionError);
        Assert.Equal("HeaderResolutionError", response.ErrorType);
        Assert.Contains("selected environment", response.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Null(executor.Request);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentDoesNotBelongToOperationDataSource_ThrowsConflict()
    {
        await using var dbContext = CreateDbContext();

        var firstDataSourceId = Guid.NewGuid();
        var secondDataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, firstDataSourceId, key: "first");
        SeedDataSource(dbContext, secondDataSourceId, key: "second");
        SeedEnvironment(dbContext, environmentId, secondDataSourceId, baseUrl: "https://other.example.com");
        SeedOperation(dbContext, operationId, firstDataSourceId);

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = CreateHandler(dbContext, executor);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None));

        Assert.Equal("The selected environment does not belong to the operation's data source.", exception.Message);
        Assert.Null(executor.Request);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentIsInactive_ThrowsConflict()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com", isActive: false);
        SeedOperation(dbContext, operationId, dataSourceId);

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = CreateHandler(dbContext, executor);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None));

        Assert.Equal("Cannot execute an operation for an inactive environment.", exception.Message);
        Assert.Null(executor.Request);
    }

    [Fact]
    public async Task Handle_WhenStructuredBearerAuthenticationIsConfigured_UsesResolvedVariableValue()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        SeedOperation(dbContext, operationId, dataSourceId, authenticationMode: OperationAuthenticationMode.Inherit);

        dbContext.Variables.Add(new Variable
        {
            Id = Guid.NewGuid(),
            DataSourceEnvironmentId = environmentId,
            Key = "AccessToken",
            Value = "secret-token",
            IsEnabled = true,
            IsSecret = true
        });

        dbContext.DataSourceAuthentications.Add(new DataSourceAuthentication
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            AuthenticationType = AuthenticationType.Bearer,
            ValueSourceType = HeaderValueSourceType.Variable,
            SourceKey = "AccessToken"
        });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = CreateHandler(dbContext, executor);

        var response = await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

        Assert.False(response.HasExecutionError);
        Assert.NotNull(executor.Request);
        Assert.Contains(executor.Request!.Headers, x => x.Key == "Authorization" && x.Value == "Bearer secret-token");
    }

    [Fact]
    public async Task Handle_WhenOperationAuthenticationModeIsNone_SkipsStructuredAuthentication()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        SeedOperation(dbContext, operationId, dataSourceId, authenticationMode: OperationAuthenticationMode.None);

        dbContext.Variables.Add(new Variable
        {
            Id = Guid.NewGuid(),
            DataSourceEnvironmentId = environmentId,
            Key = "AccessToken",
            Value = "secret-token",
            IsEnabled = true
        });

        dbContext.DataSourceAuthentications.Add(new DataSourceAuthentication
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            AuthenticationType = AuthenticationType.Bearer,
            ValueSourceType = HeaderValueSourceType.Variable,
            SourceKey = "AccessToken"
        });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = CreateHandler(dbContext, executor);

        var response = await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

        Assert.False(response.HasExecutionError);
        Assert.NotNull(executor.Request);
        Assert.DoesNotContain(executor.Request!.Headers, x => x.Key == "Authorization");
    }

    [Fact]
    public async Task Handle_WhenStructuredAuthenticationConflictsWithRawHeader_ReturnsAuthenticationConfigurationError()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        SeedOperation(dbContext, operationId, dataSourceId, authenticationMode: OperationAuthenticationMode.Inherit);

        dbContext.Headers.Add(new Header
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            Key = "Authorization",
            ValueSourceType = HeaderValueSourceType.General,
            Value = "Bearer raw-token",
            IsEnabled = true
        });

        dbContext.DataSourceAuthentications.Add(new DataSourceAuthentication
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            AuthenticationType = AuthenticationType.Bearer,
            ValueSourceType = HeaderValueSourceType.EnvironmentVariable,
            SourceKey = "ACCESS_TOKEN"
        });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = CreateHandler(dbContext, executor);

        var response = await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

        Assert.True(response.HasExecutionError);
        Assert.Equal("AuthenticationConfigurationError", response.ErrorType);
        Assert.Equal("Structured Bearer authentication conflicts with raw header 'Authorization'.", response.ErrorMessage);
        Assert.Null(executor.Request);
    }

    [Fact]
    public async Task Handle_WhenStructuredAuthenticationCannotResolve_ReturnsAuthenticationResolutionError()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        SeedOperation(dbContext, operationId, dataSourceId, authenticationMode: OperationAuthenticationMode.Inherit);

        dbContext.DataSourceAuthentications.Add(new DataSourceAuthentication
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            AuthenticationType = AuthenticationType.ApiKey,
            ValueSourceType = HeaderValueSourceType.Variable,
            SourceKey = "MissingApiKey",
            ApiKeyHeaderName = "X-Api-Key"
        });

        await dbContext.SaveChangesAsync();

        var executor = new CapturingExecutor();
        var handler = CreateHandler(dbContext, executor);

        var response = await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

        Assert.True(response.HasExecutionError);
        Assert.Equal("AuthenticationResolutionError", response.ErrorType);
        Assert.Equal("Unable to resolve API key source 'MissingApiKey'.", response.ErrorMessage);
        Assert.Null(executor.Request);
    }

    [Fact]
    public async Task Handle_WhenStructuredAuthenticationUsesExternalSource_AddsApiKeyHeader()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");
        SeedOperation(dbContext, operationId, dataSourceId, authenticationMode: OperationAuthenticationMode.Inherit);

        dbContext.DataSourceAuthentications.Add(new DataSourceAuthentication
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            AuthenticationType = AuthenticationType.ApiKey,
            ValueSourceType = HeaderValueSourceType.UserSecret,
            SourceKey = "Secrets:ApiKey",
            ApiKeyHeaderName = "X-Api-Key"
        });

        await dbContext.SaveChangesAsync();

        var resolver = new StubExternalHeaderValueResolver(
            userSecrets: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Secrets:ApiKey"] = "api-key-123"
            });

        var executor = new CapturingExecutor();
        var handler = new ExecuteOperationCommandHandler(dbContext, resolver, executor);

        var response = await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

        Assert.False(response.HasExecutionError);
        Assert.NotNull(executor.Request);
        Assert.Contains(executor.Request!.Headers, x => x.Key == "X-Api-Key" && x.Value == "api-key-123");
    }

    [Fact]
    public async Task Handle_UsesNullContentType_WhenOperationDoesNotSpecifyOne()
    {
        await using var dbContext = CreateDbContext();

        var dataSourceId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        SeedDataSource(dbContext, dataSourceId);
        SeedEnvironment(dbContext, environmentId, dataSourceId, baseUrl: "https://example.com");

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
        var handler = CreateHandler(dbContext, executor);

        await handler.Handle(new ExecuteOperationCommand(operationId, environmentId), CancellationToken.None);

        Assert.NotNull(executor.Request);
        Assert.Equal("https://example.com/posts", executor.Request!.Url);
        Assert.Null(executor.Request.ContentType);
    }

    private static ExecuteOperationCommandHandler CreateHandler(AppDbContext dbContext, CapturingExecutor executor)
        => new(dbContext, new StubExternalHeaderValueResolver(), executor);

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

    private static void SeedOperation(
        AppDbContext dbContext,
        Guid operationId,
        Guid dataSourceId,
        HttpMethodType methodType = HttpMethodType.Get,
        string endpoint = "/posts",
        string? body = null,
        string? contentType = null,
        OperationAuthenticationMode authenticationMode = OperationAuthenticationMode.Inherit)
    {
        dbContext.Operations.Add(new Operation
        {
            Id = operationId,
            DataSourceId = dataSourceId,
            ApiName = "Get posts",
            Endpoint = endpoint,
            MethodType = methodType,
            Body = body,
            ContentType = contentType,
            AuthenticationMode = authenticationMode
        });
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

        public Task<string?> ResolveAsync(HeaderValueSourceType valueSourceType, string sourceKey, CancellationToken cancellationToken)
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
