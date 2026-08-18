using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
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
            new { key = "jsonplaceholder" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        Assert.NotNull(created);

        var getResponse = await client.GetAsync($"/api/data-sources/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var dataSource = await getResponse.Content.ReadFromJsonAsync<DataSourceDto>();
        Assert.NotNull(dataSource);
        Assert.Equal(created.Id, dataSource!.Id);
        Assert.Equal("jsonplaceholder", dataSource.Key);
        Assert.True(dataSource.IsActive);
    }

    [Fact]
    public async Task DataSource_DuplicateKey_ReturnsConflictProblemDetails()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        await client.PostAsJsonAsync("/api/data-sources", new { key = "jsonplaceholder" });

        var response = await client.PostAsJsonAsync("/api/data-sources", new { key = "jsonplaceholder" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Equal("Conflict", problem.Title);
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
    public async Task Environment_Create_Get_Update_And_Delete_Work()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/environments",
            new { name = " Development ", baseUrl = "https://dev.remote.test/", isActive = false });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        Assert.NotNull(created);

        var listResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/environments");
        var environments = await listResponse.Content.ReadFromJsonAsync<List<DataSourceEnvironmentDto>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(environments);
        Assert.Single(environments!);
        Assert.Equal("Development", environments[0].Name);
        Assert.Equal("https://dev.remote.test", environments[0].BaseUrl);
        Assert.False(environments[0].IsActive);

        var getResponse = await client.GetAsync($"/api/environments/{created!.Id}");
        var environment = await getResponse.Content.ReadFromJsonAsync<DataSourceEnvironmentDto>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(environment);
        Assert.Equal(dataSourceId, environment!.DataSourceId);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/environments/{created.Id}",
            new { name = "Development", baseUrl = "https://updated.remote.test", isActive = true });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var updatedGetResponse = await client.GetAsync($"/api/environments/{created.Id}");
        var updatedEnvironment = await updatedGetResponse.Content.ReadFromJsonAsync<DataSourceEnvironmentDto>();

        Assert.NotNull(updatedEnvironment);
        Assert.Equal("https://updated.remote.test", updatedEnvironment!.BaseUrl);
        Assert.True(updatedEnvironment.IsActive);

        var deleteResponse = await client.DeleteAsync($"/api/environments/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var emptyListResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/environments");
        var emptyList = await emptyListResponse.Content.ReadFromJsonAsync<List<DataSourceEnvironmentDto>>();

        Assert.NotNull(emptyList);
        Assert.Empty(emptyList!);
    }

    [Fact]
    public async Task Environment_DuplicateName_IgnoresCase_WithinSameDataSource()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/environments",
            new { name = "Development", baseUrl = "https://dev.remote.test", isActive = true });

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/environments",
            new { name = "development", baseUrl = "https://dev-two.remote.test", isActive = true });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Environment_SameName_AllowedAcrossDifferentDataSources()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var firstDataSourceId = await CreateDataSourceAsync(client, "first-source");
        var secondDataSourceId = await CreateDataSourceAsync(client, "second-source");

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/data-sources/{firstDataSourceId}/environments",
            new { name = "Development", baseUrl = "https://first.remote.test", isActive = true });
        var secondResponse = await client.PostAsJsonAsync(
            $"/api/data-sources/{secondDataSourceId}/environments",
            new { name = "Development", baseUrl = "https://second.remote.test", isActive = true });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Environment_InvalidBaseUrl_ReturnsValidationProblemDetails()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/environments",
            new { name = "Development", baseUrl = "not-a-url", isActive = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemDocumentAsync(response);
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Validation failed", problem.RootElement.GetProperty("title").GetString());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("BaseUrl", out _));
    }

    [Fact]
    public async Task Environment_Create_ForNonexistentDataSource_ReturnsNotFound()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            $"/api/data-sources/{Guid.NewGuid()}/environments",
            new { name = "Development", baseUrl = "https://remote.test", isActive = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Variable_Create_Get_Update_And_Delete_Work()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = " ApiToken ", value = "secret-value", isEnabled = false });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        Assert.NotNull(created);

        var listResponse = await client.GetAsync($"/api/environments/{environmentId}/variables");
        var variables = await listResponse.Content.ReadFromJsonAsync<List<VariableDto>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(variables);
        Assert.Single(variables!);
        Assert.Equal("ApiToken", variables[0].Key);
        Assert.False(variables[0].IsEnabled);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/variables/{created!.Id}",
            new { key = "ApiToken", value = "updated-value", isEnabled = true });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var updatedListResponse = await client.GetAsync($"/api/environments/{environmentId}/variables");
        var updatedVariables = await updatedListResponse.Content.ReadFromJsonAsync<List<VariableDto>>();

        Assert.NotNull(updatedVariables);
        Assert.Equal("updated-value", updatedVariables![0].Value);
        Assert.True(updatedVariables[0].IsEnabled);

        var deleteResponse = await client.DeleteAsync($"/api/variables/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var emptyListResponse = await client.GetAsync($"/api/environments/{environmentId}/variables");
        var emptyList = await emptyListResponse.Content.ReadFromJsonAsync<List<VariableDto>>();

        Assert.NotNull(emptyList);
        Assert.Empty(emptyList!);
    }

    [Fact]
    public async Task Variable_DuplicateKey_IgnoresCase_WithinSameEnvironment()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiToken", value = "one", isEnabled = true });

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "apitoken", value = "two", isEnabled = true });

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Variable_SameKey_AllowedAcrossDifferentEnvironments()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var firstEnvironmentId = await CreateEnvironmentAsync(client, dataSourceId, name: "Development", baseUrl: "https://dev.remote.test");
        var secondEnvironmentId = await CreateEnvironmentAsync(client, dataSourceId, name: "Production", baseUrl: "https://prod.remote.test");

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/environments/{firstEnvironmentId}/variables",
            new { key = "ApiToken", value = "one", isEnabled = true });
        var secondResponse = await client.PostAsJsonAsync(
            $"/api/environments/{secondEnvironmentId}/variables",
            new { key = "ApiToken", value = "two", isEnabled = true });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
    }

    [Fact]
    public async Task DataSourceHeader_Create_Get_Update_And_Delete_Work()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new
            {
                key = " Authorization ",
                valueSourceType = 1,
                value = "Bearer token",
                sourceKey = (string?)null,
                isEnabled = false
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        Assert.NotNull(created);

        var listResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/headers");
        var headers = await listResponse.Content.ReadFromJsonAsync<List<HeaderDto>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(headers);
        Assert.Single(headers!);
        Assert.Equal("Authorization", headers[0].Key);
        Assert.False(headers[0].IsEnabled);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/headers/{created!.Id}",
            new
            {
                key = "Authorization",
                valueSourceType = 1,
                value = "Bearer updated",
                sourceKey = (string?)null,
                isEnabled = true
            });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var updatedListResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/headers");
        var updatedHeaders = await updatedListResponse.Content.ReadFromJsonAsync<List<HeaderDto>>();

        Assert.NotNull(updatedHeaders);
        Assert.Equal("Bearer updated", updatedHeaders![0].Value);
        Assert.True(updatedHeaders[0].IsEnabled);

        var deleteResponse = await client.DeleteAsync($"/api/headers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Header_DuplicateKeys_AreRejectedWithinSameScope_ButAllowedAcrossScopes()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "Authorization", valueSourceType = 1, value = "one", sourceKey = (string?)null, isEnabled = true });

        var duplicateDataSourceResponse = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "authorization", valueSourceType = 1, value = "two", sourceKey = (string?)null, isEnabled = true });

        Assert.Equal(HttpStatusCode.Conflict, duplicateDataSourceResponse.StatusCode);

        var operationResponse = await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/headers",
            new { key = "authorization", valueSourceType = 1, value = "operation", sourceKey = (string?)null, isEnabled = true });

        Assert.Equal(HttpStatusCode.Created, operationResponse.StatusCode);
    }

    [Fact]
    public async Task Header_ReservedKeys_AreRejected()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "content-type", valueSourceType = 1, value = "application/json", sourceKey = (string?)null, isEnabled = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Execute_SendsResolvedHeaders_AndOperationOverrideWins()
    {
        using var remote = new RemoteHttpStub
        {
            Responder = request => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok", Encoding.UTF8, "text/plain")
            })
        };

        using var factory = new SimpleApiTesterApiFactory(remote, configurationValues: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Secrets:ApiKey"] = "secret-456"
        });
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiToken", value = "token-123", isEnabled = true });

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "Authorization", valueSourceType = 2, value = (string?)null, sourceKey = "apitoken", isEnabled = true });
        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "X-Secret", valueSourceType = 3, value = (string?)null, sourceKey = "Secrets:ApiKey", isEnabled = true });
        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/headers",
            new { key = "authorization", valueSourceType = 1, value = "Bearer override", sourceKey = (string?)null, isEnabled = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(remote.Requests);
        Assert.Contains(
            remote.Requests[0].Headers,
            x => string.Equals(x.Key, "authorization", StringComparison.OrdinalIgnoreCase)
                && x.Value == "Bearer override");
        Assert.Contains(
            remote.Requests[0].Headers,
            x => string.Equals(x.Key, "X-Secret", StringComparison.OrdinalIgnoreCase)
                && x.Value == "secret-456");
    }

    [Fact]
    public async Task Execute_WhenHeaderCannotResolve_ReturnsExecutionError_AndSkipsTargetCall()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "Authorization", valueSourceType = 2, value = (string?)null, sourceKey = "MissingToken", isEnabled = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("HeaderResolutionError", result.ErrorType);
        Assert.Empty(remote.Requests);
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

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "name", value = "Tom & Jerry", isEnabled = true });
        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "debug", value = "true", isEnabled = false });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);

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

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(
            client,
            dataSourceId,
            apiName: "Create post",
            endpoint: "/posts",
            methodType: 2,
            body: "{\"title\":\"hello\"}",
            contentType: "application/json");

        var response = await ExecuteOperationAsync(client, operationId, environmentId);

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
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
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

        await ExecuteOperationAsync(client, textOperationId, environmentId);
        await ExecuteOperationAsync(client, nullBodyOperationId, environmentId);
        await ExecuteOperationAsync(client, emptyBodyOperationId, environmentId);

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
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var notFoundOperationId = await CreateOperationAsync(client, dataSourceId, endpoint: "/not-found");
        var serverErrorOperationId = await CreateOperationAsync(client, dataSourceId, endpoint: "/server-error");

        var notFoundResponse = await ExecuteOperationAsync(client, notFoundOperationId, environmentId);
        var serverErrorResponse = await ExecuteOperationAsync(client, serverErrorOperationId, environmentId);

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

        var dataSourceId = await CreateDataSourceAsync(client, "jsonplaceholder");
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}",
            new { key = "jsonplaceholder", isActive = false });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var response = await ExecuteOperationAsync(client, operationId, environmentId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
    }

    [Fact]
    public async Task Execute_InactiveEnvironment_ReturnsConflictProblemDetails()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId, isActive: false);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        var response = await ExecuteOperationAsync(client, operationId, environmentId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
    }

    [Fact]
    public async Task Execute_WrongDataSourceEnvironment_ReturnsConflictProblemDetails()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var firstDataSourceId = await CreateDataSourceAsync(client, "first-source");
        var secondDataSourceId = await CreateDataSourceAsync(client, "second-source");
        var wrongEnvironmentId = await CreateEnvironmentAsync(client, secondDataSourceId);
        var operationId = await CreateOperationAsync(client, firstDataSourceId);

        var response = await ExecuteOperationAsync(client, operationId, wrongEnvironmentId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem!.Status);
        Assert.Contains("selected environment", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Execute_UsesSelectedEnvironmentVariableAndBaseUrl()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var developmentEnvironmentId = await CreateEnvironmentAsync(client, dataSourceId, name: "Development", baseUrl: "https://dev.remote.test");
        var productionEnvironmentId = await CreateEnvironmentAsync(client, dataSourceId, name: "Production", baseUrl: "https://prod.remote.test");
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "Authorization", valueSourceType = 2, value = (string?)null, sourceKey = "AccessToken", isEnabled = true });

        await client.PostAsJsonAsync(
            $"/api/environments/{developmentEnvironmentId}/variables",
            new { key = "AccessToken", value = "DEV_TOKEN", isEnabled = true });
        await client.PostAsJsonAsync(
            $"/api/environments/{productionEnvironmentId}/variables",
            new { key = "AccessToken", value = "PROD_TOKEN", isEnabled = true });

        await ExecuteOperationAsync(client, operationId, developmentEnvironmentId);
        await ExecuteOperationAsync(client, operationId, productionEnvironmentId);

        Assert.Equal(2, remote.Requests.Count);
        Assert.Equal("https://dev.remote.test/posts", remote.Requests[0].Url);
        Assert.Contains(remote.Requests[0].Headers, x => x.Key == "Authorization" && x.Value == "DEV_TOKEN");
        Assert.Equal("https://prod.remote.test/posts", remote.Requests[1].Url);
        Assert.Contains(remote.Requests[1].Headers, x => x.Key == "Authorization" && x.Value == "PROD_TOKEN");
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

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        var response = await ExecuteOperationAsync(client, operationId, environmentId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();
        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("HttpRequestError", result.ErrorType);
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public async Task Delete_DataSource_DeletesEnvironmentsAndVariables()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiToken", value = "one", isEnabled = true });

        var deleteResponse = await client.DeleteAsync($"/api/data-sources/{dataSourceId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var environmentResponse = await client.GetAsync($"/api/environments/{environmentId}");
        Assert.Equal(HttpStatusCode.NotFound, environmentResponse.StatusCode);

        var variablesResponse = await client.GetAsync($"/api/environments/{environmentId}/variables");
        Assert.Equal(HttpStatusCode.NotFound, variablesResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Environment_DeletesVariables()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiToken", value = "one", isEnabled = true });

        var deleteResponse = await client.DeleteAsync($"/api/environments/{environmentId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var variablesResponse = await client.GetAsync($"/api/environments/{environmentId}/variables");
        Assert.Equal(HttpStatusCode.NotFound, variablesResponse.StatusCode);
    }

    private static async Task<Guid> CreateDataSourceAsync(HttpClient client, string? key = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/data-sources",
            new { key = key ?? $"jsonplaceholder-{Guid.NewGuid():N}" });

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreatedIdResponse>();
        return created!.Id;
    }

    private static async Task<Guid> CreateEnvironmentAsync(
        HttpClient client,
        Guid dataSourceId,
        string name = "Development",
        string baseUrl = "https://remote.test",
        bool isActive = true)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/environments",
            new
            {
                name,
                baseUrl,
                isActive
            });

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

    private static Task<HttpResponseMessage> ExecuteOperationAsync(HttpClient client, Guid operationId, Guid environmentId)
        => client.PostAsync($"/api/operations/{operationId}/execute?environmentId={environmentId}", null);

    private static async Task<JsonDocument> ReadProblemDocumentAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static async Task<StoredVariableState> GetStoredVariableStateAsync(SimpleApiTesterApiFactory factory, Guid variableId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var variable = await dbContext.Variables
            .AsNoTracking()
            .Where(x => x.Id == variableId)
            .Select(x => new StoredVariableState(x.Key, x.Value, x.IsEnabled, x.IsSecret))
            .SingleAsync();

        return variable;
    }

    private sealed record CreatedIdResponse(Guid Id);

    private sealed record DataSourceDto(Guid Id, string Key, bool IsActive);

    private sealed record DataSourceEnvironmentDto(Guid Id, Guid DataSourceId, string Name, string BaseUrl, bool IsActive);

    private sealed record OperationDto(
        Guid Id,
        Guid DataSourceId,
        string ApiName,
        string Endpoint,
        int MethodType,
        string? Body,
        string? ContentType);

    private sealed record QueryParameterDto(Guid Id, Guid OperationId, string Key, string? Value, bool IsEnabled);

    private sealed record VariableDto(Guid Id, Guid DataSourceEnvironmentId, string Key, string? Value, bool IsEnabled, bool IsSecret);

    private sealed record StoredVariableState(string Key, string? Value, bool IsEnabled, bool IsSecret);

    private sealed record HeaderDto(
        Guid Id,
        Guid? DataSourceId,
        Guid? OperationId,
        string Key,
        string? Value,
        int ValueSourceType,
        string? SourceKey,
        bool IsEnabled);

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
        private readonly IReadOnlyDictionary<string, string?> _configurationValues;
        private readonly RemoteHttpStub _remoteHttpStub;

        public SimpleApiTesterApiFactory(
            RemoteHttpStub? remoteHttpStub = null,
            IReadOnlyDictionary<string, string?>? configurationValues = null)
        {
            _remoteHttpStub = remoteHttpStub ?? new RemoteHttpStub();
            _configurationValues = configurationValues ?? new Dictionary<string, string?>();
        }

        public HttpClient CreateApiClient()
            => CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(_configurationValues);
            });

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
                request.Content?.Headers.ContentType?.ToString(),
                request.Headers.SelectMany(
                        header => header.Value.Select(value => new KeyValuePair<string, string>(header.Key, value)))
                    .ToList()));

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
        public CapturedRequest(
            HttpMethod method,
            string url,
            string? body,
            string? contentType,
            IReadOnlyList<KeyValuePair<string, string>> headers)
        {
            Method = method;
            Url = url;
            Body = body;
            ContentType = contentType;
            Headers = headers;
        }

        public HttpMethod Method { get; }

        public string Url { get; }

        public string? Body { get; }

        public string? ContentType { get; }

        public IReadOnlyList<KeyValuePair<string, string>> Headers { get; }

        public void Dispose()
        {
        }
    }
}
