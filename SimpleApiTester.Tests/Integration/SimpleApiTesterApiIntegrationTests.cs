using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleApiTester.Application.Abstractions.Persistence;
using SimpleApiTester.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SimpleApiTester.Tests.Integration;

public sealed class SimpleApiTesterApiIntegrationTests
{
    [Fact]
    public async Task DataSource_Post_And_Get_ReturnExpectedPayload()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/data-sources",
            new { key = "jsonplaceholder", baseUrl = "https://remote.test" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        Assert.NotNull(created);

        var getResponse = await client.GetAsync($"/api/data-sources/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var dataSource = await getResponse.Content.ReadFromJsonAsync<DataSourceDto>();
        Assert.NotNull(dataSource);
        Assert.Equal(created.Id, dataSource!.Id);
        Assert.Equal("jsonplaceholder", dataSource.Key);
        Assert.Equal("https://remote.test", dataSource.BaseUrl);
        Assert.True(dataSource.IsActive);
    }

    [Fact]
    public async Task DataSource_DuplicateKey_ReturnsConflictProblemDetails()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        await client.PostAsJsonAsync(
            "/api/data-sources",
            new { key = "jsonplaceholder", baseUrl = "https://remote.test" });

        var response = await client.PostAsJsonAsync(
            "/api/data-sources",
            new { key = "jsonplaceholder", baseUrl = "https://remote-two.test" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("Conflict", problem.Title);
    }

    [Fact]
    public async Task DataSource_InvalidBaseUrl_ReturnsValidationProblemDetails()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            "/api/data-sources",
            new { key = "jsonplaceholder", baseUrl = "not-a-url" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemDocumentAsync(response);
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Validation failed", problem.RootElement.GetProperty("title").GetString());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("BaseUrl", out _));
    }

    [Fact]
    public async Task Operation_Create_And_Get_Work()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/operations",
            new
            {
                apiName = "Get posts",
                endpoint = "/posts",
                methodType = 1,
                body = (string?)null,
                contentType = (string?)null
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        Assert.NotNull(created);

        var getResponse = await client.GetAsync($"/api/operations/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var operation = await getResponse.Content.ReadFromJsonAsync<OperationDto>();
        Assert.NotNull(operation);
        Assert.Equal(created.Id, operation!.Id);
        Assert.Equal(dataSourceId, operation.DataSourceId);
        Assert.Equal("Get posts", operation.ApiName);
        Assert.Equal("/posts", operation.Endpoint);
    }

    [Fact]
    public async Task Operation_Create_ForNonexistentDataSource_ReturnsNotFound()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            $"/api/data-sources/{Guid.NewGuid()}/operations",
            new
            {
                apiName = "Get posts",
                endpoint = "/posts",
                methodType = 1,
                body = (string?)null,
                contentType = (string?)null
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Operation_InvalidEndpointWithQueryString_ReturnsValidationProblemDetails()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/operations",
            new
            {
                apiName = "Get posts",
                endpoint = "/posts?userId=1",
                methodType = 1,
                body = (string?)null,
                contentType = (string?)null
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemDocumentAsync(response);
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("Endpoint", out _));
    }

    [Fact]
    public async Task QueryParameter_Create_Get_And_DisabledState_Work()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var operationId = await CreateOperationAsync(client, await CreateDataSourceAsync(client));

        var createResponse = await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "debug", value = "true", isEnabled = false });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/operations/{operationId}/query-parameters");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var queryParameters = await listResponse.Content.ReadFromJsonAsync<List<QueryParameterDto>>();
        Assert.NotNull(queryParameters);
        Assert.Single(queryParameters!);
        Assert.Equal("debug", queryParameters[0].Key);
        Assert.Equal("true", queryParameters[0].Value);
        Assert.False(queryParameters[0].IsEnabled);
    }

    [Fact]
    public async Task QueryParameter_Create_ForNonexistentOperation_ReturnsNotFound()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            $"/api/operations/{Guid.NewGuid()}/query-parameters",
            new { key = "debug", value = "true", isEnabled = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Execute_Get_Success_EncodesQuery_AndExcludesDisabledParameters()
    {
        using var remote = new RemoteHttpStub
        {
            Responder = request => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok", Encoding.UTF8, "text/plain")
            })
        };

        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var operationId = await CreateOperationAsync(client, await CreateDataSourceAsync(client));

        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "name", value = "Tom & Jerry", isEnabled = true });
        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "debug", value = "true", isEnabled = false });

        var response = await client.PostAsync($"/api/operations/{operationId}/execute", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();
        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", result.ContentType);

        Assert.Single(remote.Requests);
        Assert.Equal("https://remote.test/posts?name=Tom%20%26%20Jerry", remote.Requests[0].Url);
        Assert.Equal(HttpMethod.Get, remote.Requests[0].Method);
        Assert.Null(remote.Requests[0].Body);
    }

    [Fact]
    public async Task Execute_Post_WithJsonBody_PassesBodyAndContentType()
    {
        using var remote = new RemoteHttpStub
        {
            Responder = request => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("created", Encoding.UTF8, "application/json")
            })
        };

        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var operationId = await CreateOperationAsync(
            client,
            await CreateDataSourceAsync(client),
            apiName: "Create post",
            endpoint: "/posts",
            methodType: 2,
            body: "{\"title\":\"hello\"}",
            contentType: "application/json");

        var response = await client.PostAsync($"/api/operations/{operationId}/execute", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(remote.Requests);
        Assert.Equal(HttpMethod.Post, remote.Requests[0].Method);
        Assert.Equal("{\"title\":\"hello\"}", remote.Requests[0].Body);
        Assert.Equal("application/json; charset=utf-8", remote.Requests[0].ContentType);
    }

    [Fact]
    public async Task Execute_ContentTypeAndBodySemantics_ArePreserved()
    {
        using var remote = new RemoteHttpStub
        {
            Responder = request => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok", Encoding.UTF8, "text/plain")
            })
        };

        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var textOperationId = await CreateOperationAsync(
            client,
            dataSourceId,
            apiName: "Plain text",
            endpoint: "/text",
            methodType: 2,
            body: "hello",
            contentType: "text/plain");
        var nullBodyOperationId = await CreateOperationAsync(
            client,
            dataSourceId,
            apiName: "Null body",
            endpoint: "/null-body",
            methodType: 2,
            body: null,
            contentType: null);
        var emptyBodyOperationId = await CreateOperationAsync(
            client,
            dataSourceId,
            apiName: "Empty body",
            endpoint: "/empty-body",
            methodType: 2,
            body: string.Empty,
            contentType: "text/plain");

        await client.PostAsync($"/api/operations/{textOperationId}/execute", null);
        await client.PostAsync($"/api/operations/{nullBodyOperationId}/execute", null);
        await client.PostAsync($"/api/operations/{emptyBodyOperationId}/execute", null);

        Assert.Equal(3, remote.Requests.Count);
        Assert.Equal("text/plain; charset=utf-8", remote.Requests[0].ContentType);
        Assert.Equal("hello", remote.Requests[0].Body);
        Assert.Null(remote.Requests[1].Body);
        Assert.Null(remote.Requests[1].ContentType);
        Assert.Equal(string.Empty, remote.Requests[2].Body);
        Assert.Equal("text/plain; charset=utf-8", remote.Requests[2].ContentType);
    }

    [Fact]
    public async Task Execute_Remote404And500_AreReturnedAsExecutionResults()
    {
        using var remote = new RemoteHttpStub
        {
            Responder = request =>
            {
                var statusCode = request.Url.Contains("server-error", StringComparison.Ordinal)
                    ? HttpStatusCode.InternalServerError
                    : HttpStatusCode.NotFound;

                return Task.FromResult(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(statusCode.ToString(), Encoding.UTF8, "text/plain")
                });
            }
        };

        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var notFoundOperationId = await CreateOperationAsync(client, dataSourceId, endpoint: "/not-found");
        var serverErrorOperationId = await CreateOperationAsync(client, dataSourceId, endpoint: "/server-error");

        var notFoundResponse = await client.PostAsync($"/api/operations/{notFoundOperationId}/execute", null);
        var serverErrorResponse = await client.PostAsync($"/api/operations/{serverErrorOperationId}/execute", null);

        var notFoundResult = await notFoundResponse.Content.ReadFromJsonAsync<ExecuteOperationDto>();
        var serverErrorResult = await serverErrorResponse.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.Equal(HttpStatusCode.OK, notFoundResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, serverErrorResponse.StatusCode);
        Assert.Equal(404, notFoundResult!.StatusCode);
        Assert.False(notFoundResult.HasExecutionError);
        Assert.Equal(500, serverErrorResult!.StatusCode);
        Assert.False(serverErrorResult.HasExecutionError);
    }

    [Fact]
    public async Task Execute_InactiveDataSource_ReturnsConflictProblemDetails()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}",
            new { key = "jsonplaceholder", baseUrl = "https://remote.test", isActive = false });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var response = await client.PostAsync($"/api/operations/{operationId}/execute", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
    }

    [Fact]
    public async Task Execute_HttpRequestFailure_ReturnsExecutionErrorResult()
    {
        using var remote = new RemoteHttpStub
        {
            Responder = _ => throw new HttpRequestException("Connection refused.")
        };

        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var operationId = await CreateOperationAsync(client, await CreateDataSourceAsync(client));

        var response = await client.PostAsync($"/api/operations/{operationId}/execute", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();
        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("HttpRequestError", result.ErrorType);
        Assert.Null(result.StatusCode);
    }

    private static async Task<Guid> CreateDataSourceAsync(HttpClient client, string? key = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/data-sources",
            new { key = key ?? $"jsonplaceholder-{Guid.NewGuid():N}", baseUrl = "https://remote.test" });

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreatedIdResponse>();
        return created!.Id;
    }

    private static async Task<Guid> CreateOperationAsync(
        HttpClient client,
        Guid dataSourceId,
        string apiName = "Get posts",
        string endpoint = "/posts",
        int methodType = 1,
        string? body = null,
        string? contentType = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/operations",
            new
            {
                apiName,
                endpoint,
                methodType,
                body,
                contentType
            });

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreatedIdResponse>();
        return created!.Id;
    }

    private static async Task<JsonDocument> ReadProblemDocumentAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private sealed record CreatedIdResponse(Guid Id);

    private sealed record DataSourceDto(Guid Id, string Key, string BaseUrl, bool IsActive);

    private sealed record OperationDto(
        Guid Id,
        Guid DataSourceId,
        string ApiName,
        string Endpoint,
        int MethodType,
        string? Body,
        string? ContentType);

    private sealed record QueryParameterDto(Guid Id, Guid OperationId, string Key, string? Value, bool IsEnabled);

    private sealed record ExecuteOperationDto(
        int? StatusCode,
        bool? IsSuccessStatusCode,
        string? ResponseBody,
        string? ContentType,
        long DurationMilliseconds,
        bool HasExecutionError,
        string? ErrorType,
        string? ErrorMessage);

    private sealed class SimpleApiTesterApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"SimpleApiTesterTests-{Guid.NewGuid()}";
        private readonly RemoteHttpStub _remoteHttpStub;

        public SimpleApiTesterApiFactory(RemoteHttpStub? remoteHttpStub = null)
        {
            _remoteHttpStub = remoteHttpStub ?? new RemoteHttpStub();
        }

        public HttpClient CreateApiClient()
            => CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<IAppDbContext>();
                services.RemoveAll<IHttpClientFactory>();

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));

                services.AddScoped<IAppDbContext>(provider =>
                    provider.GetRequiredService<AppDbContext>());

                services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(_remoteHttpStub));
            });
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly RemoteHttpStub _remoteHttpStub;

        public StubHttpClientFactory(RemoteHttpStub remoteHttpStub)
        {
            _remoteHttpStub = remoteHttpStub;
        }

        public HttpClient CreateClient(string name)
            => new(new StubHttpMessageHandler(_remoteHttpStub));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly RemoteHttpStub _remoteHttpStub;

        public StubHttpMessageHandler(RemoteHttpStub remoteHttpStub)
        {
            _remoteHttpStub = remoteHttpStub;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _remoteHttpStub.SendAsync(request, cancellationToken);
    }

    private sealed class RemoteHttpStub : IDisposable
    {
        public List<CapturedRequest> Requests { get; } = [];

        public Func<CapturedRequest, Task<HttpResponseMessage>> Responder { get; set; }
            = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok", Encoding.UTF8, "text/plain")
            });

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri!.AbsoluteUri,
                body,
                request.Content?.Headers.ContentType?.ToString()));

            return await Responder(Requests[^1]);
        }

        public void Dispose()
        {
            foreach (var request in Requests)
            {
                request.Dispose();
            }
        }
    }

    private sealed class CapturedRequest : IDisposable
    {
        public CapturedRequest(HttpMethod method, string url, string? body, string? contentType)
        {
            Method = method;
            Url = url;
            Body = body;
            ContentType = contentType;
        }

        public HttpMethod Method { get; }

        public string Url { get; }

        public string? Body { get; }

        public string? ContentType { get; }

        public void Dispose()
        {
        }
    }
}
