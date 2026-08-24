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
        Assert.Null(dataSource.DefaultTimeoutSeconds);
    }

    [Fact]
    public async Task DataSource_Post_WithConfiguredTimeout_ReturnExpectedPayload()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/data-sources",
            new { key = "hr-system", defaultTimeoutSeconds = 5 });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        var getResponse = await client.GetAsync($"/api/data-sources/{created!.Id}");
        var dataSource = await getResponse.Content.ReadFromJsonAsync<DataSourceDto>();

        Assert.NotNull(dataSource);
        Assert.Equal(5, dataSource!.DefaultTimeoutSeconds);
    }

    [Fact]
    public async Task DataSource_Put_UpdatesAndClearsTimeout()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client, defaultTimeoutSeconds: 5);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}",
            new { key = "jsonplaceholder", isActive = true, defaultTimeoutSeconds = 20 });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var updated = await client.GetFromJsonAsync<DataSourceDto>($"/api/data-sources/{dataSourceId}");
        Assert.NotNull(updated);
        Assert.Equal(20, updated!.DefaultTimeoutSeconds);

        var clearResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}",
            new { key = "jsonplaceholder", isActive = true, defaultTimeoutSeconds = (int?)null });

        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        var cleared = await client.GetFromJsonAsync<DataSourceDto>($"/api/data-sources/{dataSourceId}");
        Assert.NotNull(cleared);
        Assert.Null(cleared!.DefaultTimeoutSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public async Task DataSource_Post_InvalidTimeout_ReturnsBadRequest(int defaultTimeoutSeconds)
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            "/api/data-sources",
            new { key = "jsonplaceholder", defaultTimeoutSeconds });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public async Task DataSource_Put_InvalidTimeout_ReturnsBadRequest(int defaultTimeoutSeconds)
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}",
            new { key = "jsonplaceholder", isActive = true, defaultTimeoutSeconds });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(1, operation.AuthenticationMode);
    }

    [Fact]
    public async Task DataSourceAuthentication_GetMissing_ReturnsNotFound()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var response = await client.GetAsync($"/api/data-sources/{dataSourceId}/authentication");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DataSource_TestConnection_Remote200_ReturnsReachable()
    {
        using var remoteHttpStub = new RemoteHttpStub
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok", Encoding.UTF8, "text/plain")
            })
        };
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId, baseUrl: "https://remote.test");

        var response = await TestConnectionAsync(client, dataSourceId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<TestDataSourceConnectionDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result!.IsReachable);
        Assert.Equal(200, result.StatusCode);
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal("text/plain; charset=utf-8", result.ContentType);

        var request = Assert.Single(remoteHttpStub.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://remote.test/", request.Url);
        Assert.Null(request.Body);
        Assert.Null(request.ContentType);
        Assert.DoesNotContain(request.Headers, x => x.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, 404)]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.InternalServerError, 500)]
    public async Task DataSource_TestConnection_RemoteNonSuccessStillReturnsReachable(HttpStatusCode remoteStatusCode, int expectedStatusCode)
    {
        using var remoteHttpStub = new RemoteHttpStub
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(remoteStatusCode))
        };
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId, baseUrl: "https://remote.test");

        var response = await TestConnectionAsync(client, dataSourceId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<TestDataSourceConnectionDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result!.IsReachable);
        Assert.Equal(expectedStatusCode, result.StatusCode);
        Assert.False(result.IsSuccessStatusCode);
        Assert.Null(result.ErrorType);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task DataSource_TestConnection_WhenHttpRequestFails_ReturnsUnreachable()
    {
        using var remoteHttpStub = new RemoteHttpStub
        {
            Responder = _ => throw new HttpRequestException("connection refused")
        };
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId, baseUrl: "https://remote.test");

        var response = await TestConnectionAsync(client, dataSourceId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<TestDataSourceConnectionDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.False(result!.IsReachable);
        Assert.Null(result.StatusCode);
        Assert.Equal("HttpRequestError", result.ErrorType);
        Assert.Equal("Unable to connect to target.", result.ErrorMessage);
        Assert.DoesNotContain("connection refused", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DataSource_TestConnection_WhenLocalTimeoutOccurs_ReturnsTimeout()
    {
        using var remoteHttpStub = new RemoteHttpStub
        {
            Responder = _ => throw new OperationCanceledException()
        };
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client, defaultTimeoutSeconds: 2);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId, baseUrl: "https://remote.test");

        var response = await TestConnectionAsync(client, dataSourceId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<TestDataSourceConnectionDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.False(result!.IsReachable);
        Assert.Equal("Timeout", result.ErrorType);
        Assert.Equal("The HTTP request timed out.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, 408)]
    [InlineData(HttpStatusCode.GatewayTimeout, 504)]
    public async Task DataSource_TestConnection_Remote408And504_StillReturnReachable(HttpStatusCode remoteStatusCode, int expectedStatusCode)
    {
        using var remoteHttpStub = new RemoteHttpStub
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(remoteStatusCode))
        };
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client, defaultTimeoutSeconds: 2);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId, baseUrl: "https://remote.test");

        var response = await TestConnectionAsync(client, dataSourceId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<TestDataSourceConnectionDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result!.IsReachable);
        Assert.Equal(expectedStatusCode, result.StatusCode);
        Assert.False(result.IsSuccessStatusCode);
        Assert.Null(result.ErrorType);
    }

    [Fact]
    public async Task DataSource_TestConnection_InvalidDataSource_ReturnsNotFound_AndDoesNotSendHttp()
    {
        using var remoteHttpStub = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var response = await TestConnectionAsync(client, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(remoteHttpStub.Requests);
    }

    [Fact]
    public async Task DataSource_TestConnection_InvalidOrCrossDataSourceEnvironment_ReturnsNotFound_AndDoesNotSendHttp()
    {
        using var remoteHttpStub = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var firstDataSourceId = await CreateDataSourceAsync(client);
        var secondDataSourceId = await CreateDataSourceAsync(client);
        var otherEnvironmentId = await CreateEnvironmentAsync(client, secondDataSourceId, baseUrl: "https://other.test");

        var response = await TestConnectionAsync(client, firstDataSourceId, otherEnvironmentId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(remoteHttpStub.Requests);
    }

    [Fact]
    public async Task DataSource_TestConnection_InactiveDataSourceOrEnvironment_ReturnsConflict_AndDoesNotSendHttp()
    {
        using var remoteHttpStub = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var inactiveDataSourceId = await CreateDataSourceAsync(client);
        var inactiveDataSourceEnvironmentId = await CreateEnvironmentAsync(client, inactiveDataSourceId, baseUrl: "https://inactive-ds.test");

        await client.PutAsJsonAsync($"/api/data-sources/{inactiveDataSourceId}", new { key = "inactive-ds", isActive = false });

        var inactiveDataSourceResponse = await TestConnectionAsync(client, inactiveDataSourceId, inactiveDataSourceEnvironmentId);
        Assert.Equal(HttpStatusCode.Conflict, inactiveDataSourceResponse.StatusCode);

        var activeDataSourceId = await CreateDataSourceAsync(client);
        var inactiveEnvironmentId = await CreateEnvironmentAsync(client, activeDataSourceId, baseUrl: "https://inactive-env.test", isActive: false);

        var inactiveEnvironmentResponse = await TestConnectionAsync(client, activeDataSourceId, inactiveEnvironmentId);
        Assert.Equal(HttpStatusCode.Conflict, inactiveEnvironmentResponse.StatusCode);
        Assert.Empty(remoteHttpStub.Requests);
    }

    [Fact]
    public async Task Execute_WhenLocalTimeoutOccurs_ReturnsTimeoutExecutionError()
    {
        using var remoteHttpStub = new RemoteHttpStub
        {
            Responder = _ => throw new OperationCanceledException()
        };
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client, defaultTimeoutSeconds: 2);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId, baseUrl: "https://remote.test");
        var operationId = await CreateOperationAsync(client, dataSourceId);

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("Timeout", result.ErrorType);
        Assert.Equal("The HTTP request timed out.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, 408)]
    [InlineData(HttpStatusCode.GatewayTimeout, 504)]
    public async Task Execute_Remote408And504_StillReturnNormalRemoteResponses(HttpStatusCode remoteStatusCode, int expectedStatusCode)
    {
        using var remoteHttpStub = new RemoteHttpStub
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(remoteStatusCode)
            {
                Content = new StringContent("timed out", Encoding.UTF8, "text/plain")
            })
        };
        using var factory = new SimpleApiTesterApiFactory(remoteHttpStub);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client, defaultTimeoutSeconds: 2);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId, baseUrl: "https://remote.test");
        var operationId = await CreateOperationAsync(client, dataSourceId);

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Equal(expectedStatusCode, result.StatusCode);
        Assert.False(result.IsSuccessStatusCode);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_Get_Replace_And_Delete_Work()
    {
        using var factory = new SimpleApiTesterApiFactory(
            configurationValues: new Dictionary<string, string?>
            {
                ["Secrets:ApiKey"] = "from-config"
            });
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var createResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 1,
                valueSourceType = 2,
                sourceKey = "AccessToken",
                apiKeyHeaderName = (string?)null
            });

        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/authentication");
        var createdAuth = await getResponse.Content.ReadFromJsonAsync<DataSourceAuthenticationDto>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(createdAuth);
        Assert.Equal(dataSourceId, createdAuth!.DataSourceId);
        Assert.Equal(1, createdAuth.AuthenticationType);
        Assert.Equal(2, createdAuth.ValueSourceType);
        Assert.Equal("AccessToken", createdAuth.SourceKey);
        Assert.Null(createdAuth.ApiKeyHeaderName);

        var replaceResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 3,
                sourceKey = "Secrets:ApiKey",
                apiKeyHeaderName = "X-Api-Key"
            });

        Assert.Equal(HttpStatusCode.NoContent, replaceResponse.StatusCode);

        var replacedResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/authentication");
        var replacedAuth = await replacedResponse.Content.ReadFromJsonAsync<DataSourceAuthenticationDto>();

        Assert.NotNull(replacedAuth);
        Assert.Equal(2, replacedAuth!.AuthenticationType);
        Assert.Equal(3, replacedAuth.ValueSourceType);
        Assert.Equal("Secrets:ApiKey", replacedAuth.SourceKey);
        Assert.Equal("X-Api-Key", replacedAuth.ApiKeyHeaderName);

        var deleteResponse = await client.DeleteAsync($"/api/data-sources/{dataSourceId}/authentication");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/authentication");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_BearerWithConflictingRawAuthorizationHeader_ReturnsConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "Authorization", valueSourceType = 1, value = "Bearer raw", sourceKey = (string?)null, isEnabled = true });

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 1,
                valueSourceType = 2,
                sourceKey = "AccessToken",
                apiKeyHeaderName = (string?)null
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_ApiKeyWithAuthorizationHeaderName_ReturnsBadRequest()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "Authorization"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_ApiKeyQuery_Get_ReturnsMetadataOnly()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var putResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = " api_key ",
                apiKeyLocation = 2
            });

        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/authentication");
        var auth = await getResponse.Content.ReadFromJsonAsync<DataSourceAuthenticationDto>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(auth);
        Assert.Equal(2, auth!.AuthenticationType);
        Assert.Equal(2, auth.ValueSourceType);
        Assert.Equal("ApiKey", auth.SourceKey);
        Assert.Equal("api_key", auth.ApiKeyHeaderName);
        Assert.Equal(2, auth.ApiKeyLocation);
        Assert.Null(auth.UsernameSourceType);
        Assert.Null(auth.PasswordSourceType);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_LegacyApiKeyPayloadWithoutLocation_PersistsHeaderLocation()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var putResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "X-Api-Key"
            });

        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var auth = await client.GetFromJsonAsync<DataSourceAuthenticationDto>($"/api/data-sources/{dataSourceId}/authentication");

        Assert.NotNull(auth);
        Assert.Equal(1, auth!.ApiKeyLocation);
    }

    [Theory]
    [InlineData("invalid-location", "ApiKeyLocation")]
    [InlineData("missing-key", "ApiKeyHeaderName")]
    [InlineData("whitespace-key", "ApiKeyHeaderName")]
    [InlineData("cr-key", "ApiKeyHeaderName")]
    [InlineData("lf-key", "ApiKeyHeaderName")]
    [InlineData("question-key", "ApiKeyHeaderName")]
    [InlineData("ampersand-key", "ApiKeyHeaderName")]
    [InlineData("equals-key", "ApiKeyHeaderName")]
    [InlineData("general-source", "ValueSourceType")]
    public async Task DataSourceAuthentication_Put_ApiKeyQuery_InvalidValues_ReturnBadRequest(string scenario, string expectedErrorKey)
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var payload = new Dictionary<string, object?>
        {
            ["authenticationType"] = 2,
            ["valueSourceType"] = 2,
            ["sourceKey"] = "ApiKey",
            ["apiKeyHeaderName"] = "api_key",
            ["apiKeyLocation"] = 2
        };

        switch (scenario)
        {
            case "invalid-location":
                payload["apiKeyLocation"] = 99;
                break;
            case "missing-key":
                payload.Remove("apiKeyHeaderName");
                break;
            case "whitespace-key":
                payload["apiKeyHeaderName"] = "   ";
                break;
            case "cr-key":
                payload["apiKeyHeaderName"] = "api\rkey";
                break;
            case "lf-key":
                payload["apiKeyHeaderName"] = "api\nkey";
                break;
            case "question-key":
                payload["apiKeyHeaderName"] = "api?key";
                break;
            case "ampersand-key":
                payload["apiKeyHeaderName"] = "api&key";
                break;
            case "equals-key":
                payload["apiKeyHeaderName"] = "api=key";
                break;
            case "general-source":
                payload["valueSourceType"] = 1;
                break;
        }

        var response = await client.PutAsJsonAsync($"/api/data-sources/{dataSourceId}/authentication", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problem = await ReadProblemDocumentAsync(response);
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(expectedErrorKey, out _));
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_ApiKeyQuery_AllowsAuthorizationAsQueryKey()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = " Authorization ",
                apiKeyLocation = 2
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var auth = await client.GetFromJsonAsync<DataSourceAuthenticationDto>($"/api/data-sources/{dataSourceId}/authentication");
        Assert.NotNull(auth);
        Assert.Equal("Authorization", auth!.ApiKeyHeaderName);
        Assert.Equal(2, auth.ApiKeyLocation);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_ApiKeyQuery_WithEnabledExactQueryParameterConflict_ReturnsConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await SeedRawQueryParameterAsync(factory, operationId, "api_key", "raw", isEnabled: true);

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "api_key",
                apiKeyLocation = 2
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Structured API key authentication conflicts with raw query parameter 'api_key'.", problem!.Detail);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_OAuthClientCredentials_Get_ReturnsMetadataOnly()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var putResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 4,
                oauthTokenEndpoint = "https://identity.example.com/oauth/token",
                oauthClientIdSourceType = 2,
                oauthClientIdSourceKey = "ClientId",
                oauthClientSecretSourceType = 2,
                oauthClientSecretSourceKey = "ClientSecret",
                oauthScope = "read write"
            });

        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var auth = await client.GetFromJsonAsync<DataSourceAuthenticationDto>($"/api/data-sources/{dataSourceId}/authentication");

        Assert.NotNull(auth);
        Assert.Equal(4, auth!.AuthenticationType);
        Assert.Equal("https://identity.example.com/oauth/token", auth.OAuthTokenEndpoint);
        Assert.Equal(2, auth.OAuthClientIdSourceType);
        Assert.Equal("ClientId", auth.OAuthClientIdSourceKey);
        Assert.Equal(2, auth.OAuthClientSecretSourceType);
        Assert.Equal("ClientSecret", auth.OAuthClientSecretSourceKey);
        Assert.Equal("read write", auth.OAuthScope);
        Assert.Null(auth.ApiKeyHeaderName);
        Assert.Null(auth.UsernameSourceKey);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_OAuthClientCredentials_WithConflictingRawAuthorizationHeader_ReturnsConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "Authorization", valueSourceType = 1, value = "Bearer raw", sourceKey = (string?)null, isEnabled = true });

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 4,
                oauthTokenEndpoint = "https://identity.example.com/oauth/token",
                oauthClientIdSourceType = 2,
                oauthClientIdSourceKey = "ClientId",
                oauthClientSecretSourceType = 2,
                oauthClientSecretSourceKey = "ClientSecret"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_ApiKeyQuery_IgnoresDisabledAndCaseDifferentQueryParameters()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "api_key", value = "raw", isEnabled = false });
        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "API_KEY", value = "upper", isEnabled = true });

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "api_key",
                apiKeyLocation = 2
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Header_Create_EnabledAuthorizationConflictingWithStructuredBearer_ReturnsConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 1,
                valueSourceType = 2,
                sourceKey = "AccessToken",
                apiKeyHeaderName = (string?)null
            });

        var response = await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/headers",
            new { key = "authorization", valueSourceType = 1, value = "Bearer raw", sourceKey = (string?)null, isEnabled = true });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_Get_Basic_Works_AndReturnsMetadataOnly()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var putResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/authentication");
        var document = await ReadProblemOrJsonDocumentAsync(getResponse);
        var auth = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(3, auth.GetProperty("authenticationType").GetInt32());
        Assert.True(auth.GetProperty("valueSourceType").ValueKind == JsonValueKind.Null);
        Assert.True(auth.GetProperty("sourceKey").ValueKind == JsonValueKind.Null);
        Assert.True(auth.GetProperty("apiKeyHeaderName").ValueKind == JsonValueKind.Null);
        Assert.Equal(2, auth.GetProperty("usernameSourceType").GetInt32());
        Assert.Equal("ApiUsername", auth.GetProperty("usernameSourceKey").GetString());
        Assert.Equal(2, auth.GetProperty("passwordSourceType").GetInt32());
        Assert.Equal("ApiPassword", auth.GetProperty("passwordSourceKey").GetString());
        Assert.False(auth.TryGetProperty("username", out _));
        Assert.False(auth.TryGetProperty("password", out _));
    }

    [Theory]
    [InlineData("missing-username-source-type", "UsernameSourceType")]
    [InlineData("missing-username-source-key", "UsernameSourceKey")]
    [InlineData("missing-password-source-type", "PasswordSourceType")]
    [InlineData("missing-password-source-key", "PasswordSourceKey")]
    [InlineData("forbidden-value-source-type", "ValueSourceType")]
    [InlineData("forbidden-source-key", "SourceKey")]
    [InlineData("forbidden-api-key-header-name", "ApiKeyHeaderName")]
    [InlineData("username-general-source", "UsernameSourceType")]
    [InlineData("password-general-source", "PasswordSourceType")]
    public async Task DataSourceAuthentication_Put_Basic_InvalidShape_ReturnsBadRequest(string scenario, string expectedErrorKey)
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var payload = new Dictionary<string, object?>
        {
            ["authenticationType"] = 3,
            ["valueSourceType"] = null,
            ["sourceKey"] = null,
            ["apiKeyHeaderName"] = null,
            ["usernameSourceType"] = 2,
            ["usernameSourceKey"] = "ApiUsername",
            ["passwordSourceType"] = 2,
            ["passwordSourceKey"] = "ApiPassword"
        };

        switch (scenario)
        {
            case "missing-username-source-type":
                payload.Remove("usernameSourceType");
                break;
            case "missing-username-source-key":
                payload.Remove("usernameSourceKey");
                break;
            case "missing-password-source-type":
                payload.Remove("passwordSourceType");
                break;
            case "missing-password-source-key":
                payload.Remove("passwordSourceKey");
                break;
            case "forbidden-value-source-type":
                payload["valueSourceType"] = 2;
                break;
            case "forbidden-source-key":
                payload["sourceKey"] = "AccessToken";
                break;
            case "forbidden-api-key-header-name":
                payload["apiKeyHeaderName"] = "X-Api-Key";
                break;
            case "username-general-source":
                payload["usernameSourceType"] = 1;
                break;
            case "password-general-source":
                payload["passwordSourceType"] = 1;
                break;
        }

        var response = await client.PutAsJsonAsync($"/api/data-sources/{dataSourceId}/authentication", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problem = await ReadProblemDocumentAsync(response);
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(expectedErrorKey, out _));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task DataSourceAuthentication_Put_NonBasicAuthentication_RejectsBasicFields(int authenticationType)
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var payload = new Dictionary<string, object?>
        {
            ["authenticationType"] = authenticationType,
            ["valueSourceType"] = 2,
            ["sourceKey"] = authenticationType == 1 ? "AccessToken" : "ApiKey",
            ["apiKeyHeaderName"] = authenticationType == 2 ? "X-Api-Key" : null,
            ["usernameSourceType"] = 2,
            ["usernameSourceKey"] = "ApiUsername",
            ["passwordSourceType"] = 2,
            ["passwordSourceKey"] = "ApiPassword"
        };

        var response = await client.PutAsJsonAsync($"/api/data-sources/{dataSourceId}/authentication", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var problem = await ReadProblemDocumentAsync(response);
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("UsernameSourceType", out _));
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("PasswordSourceType", out _));
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_BasicWithConflictingDataSourceAuthorizationHeader_ReturnsConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "authorization", valueSourceType = 1, value = "Basic raw", sourceKey = (string?)null, isEnabled = true });

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_BasicWithConflictingOperationAuthorizationHeader_ReturnsConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/headers",
            new { key = "Authorization", valueSourceType = 1, value = "Basic raw", sourceKey = (string?)null, isEnabled = true });

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DataSourceAuthentication_Put_Basic_IgnoresDisabledAuthorizationHeaderConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "Authorization", valueSourceType = 1, value = "Basic raw", sourceKey = (string?)null, isEnabled = false });

        var response = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Header_Update_EnablingAuthorizationConflictingWithStructuredBasic_ReturnsConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);

        var createHeaderResponse = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "authorization", valueSourceType = 1, value = "Basic raw", sourceKey = (string?)null, isEnabled = false });

        var createdHeader = await createHeaderResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        Assert.NotNull(createdHeader);

        var authResponse = await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        Assert.Equal(HttpStatusCode.NoContent, authResponse.StatusCode);

        var enableResponse = await client.PutAsJsonAsync(
            $"/api/headers/{createdHeader!.Id}",
            new { key = "Authorization", valueSourceType = 1, value = "Basic raw", sourceKey = (string?)null, isEnabled = true });

        Assert.Equal(HttpStatusCode.Conflict, enableResponse.StatusCode);
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
    public async Task QueryParameter_Create_EnabledExactKeyConflictingWithStructuredApiKeyQuery_ReturnsConflict()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "api_key",
                apiKeyLocation = 2
            });

        var response = await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "api_key", value = "raw", isEnabled = true });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task QueryParameter_Update_ConflictingKeyOrEnable_ReturnsConflict_ButDisablingIsAllowed()
    {
        using var factory = new SimpleApiTesterApiFactory();
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "api_key",
                apiKeyLocation = 2
            });

        var pageCreateResponse = await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "page", value = "1", isEnabled = true });
        var pageId = (await pageCreateResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var disabledCreateResponse = await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "api_key", value = "raw", isEnabled = false });
        var disabledId = (await disabledCreateResponse.Content.ReadFromJsonAsync<CreatedIdResponse>())!.Id;

        var conflictingKeyResponse = await client.PutAsJsonAsync(
            $"/api/query-parameters/{pageId}",
            new { operationId, key = "api_key", value = "1", isEnabled = true });

        Assert.Equal(HttpStatusCode.Conflict, conflictingKeyResponse.StatusCode);

        var enableConflictResponse = await client.PutAsJsonAsync(
            $"/api/query-parameters/{disabledId}",
            new { operationId, key = "api_key", value = "raw", isEnabled = true });

        Assert.Equal(HttpStatusCode.Conflict, enableConflictResponse.StatusCode);

        var disableAllowedResponse = await client.PutAsJsonAsync(
            $"/api/query-parameters/{disabledId}",
            new { operationId, key = "api_key", value = "raw", isEnabled = false });

        Assert.Equal(HttpStatusCode.NoContent, disableAllowedResponse.StatusCode);
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
    public async Task Execute_StructuredOAuthClientCredentials_AcquiresToken_AndSendsBearerHeader()
    {
        using var remote = new RemoteHttpStub
        {
            Responder = request =>
            {
                if (request.Url == "https://identity.example.com/oauth/token")
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"access_token\":\"oauth-token-123\",\"token_type\":\"Bearer\"}", Encoding.UTF8, "application/json")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("ok", Encoding.UTF8, "text/plain")
                });
            }
        };

        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ClientId", value = "client id", isEnabled = true, isSecret = false });
        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ClientSecret", value = "secret+value&x=y", isEnabled = true, isSecret = true });

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 4,
                oauthTokenEndpoint = "https://identity.example.com/oauth/token",
                oauthClientIdSourceType = 2,
                oauthClientIdSourceKey = "ClientId",
                oauthClientSecretSourceType = 2,
                oauthClientSecretSourceKey = "ClientSecret",
                oauthScope = "read write"
            });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Equal(2, remote.Requests.Count);

        var tokenRequest = remote.Requests[0];
        var targetRequest = remote.Requests[1];
        var form = ParseFormUrlEncoded(tokenRequest.Body!);

        Assert.Equal(HttpMethod.Post, tokenRequest.Method);
        Assert.Equal("https://identity.example.com/oauth/token", tokenRequest.Url);
        Assert.Equal("application/x-www-form-urlencoded", tokenRequest.ContentType);
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal("client id", form["client_id"]);
        Assert.Equal("secret+value&x=y", form["client_secret"]);
        Assert.Equal("read write", form["scope"]);
        Assert.Equal("https://remote.test/posts", targetRequest.Url);
        Assert.Contains(targetRequest.Headers, x => string.Equals(x.Key, "Authorization", StringComparison.OrdinalIgnoreCase) && x.Value == "Bearer oauth-token-123");
    }

    [Fact]
    public async Task Execute_StructuredOAuthClientCredentials_WhenTokenEndpointReturns401_ReturnsOAuthTokenError_AndSkipsTargetCall()
    {
        using var remote = new RemoteHttpStub
        {
            Responder = request =>
            {
                if (request.Url == "https://identity.example.com/oauth/token")
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        };

        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ClientId", value = "client-id", isEnabled = true, isSecret = false });
        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ClientSecret", value = "client-secret", isEnabled = true, isSecret = true });

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 4,
                oauthTokenEndpoint = "https://identity.example.com/oauth/token",
                oauthClientIdSourceType = 2,
                oauthClientIdSourceKey = "ClientId",
                oauthClientSecretSourceType = 2,
                oauthClientSecretSourceKey = "ClientSecret"
            });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("OAuthTokenError", result.ErrorType);
        Assert.Equal("OAuth token request returned HTTP 401.", result.ErrorMessage);
        Assert.Single(remote.Requests);
        Assert.Equal("https://identity.example.com/oauth/token", remote.Requests[0].Url);
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
    public async Task Execute_StructuredBearer_FromSecretVariable_UsesRealValue_AndMasksVariableReads()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 1,
                valueSourceType = 2,
                sourceKey = "AccessToken",
                apiKeyHeaderName = (string?)null
            });

        var createVariableResponse = await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "AccessToken", value = "REAL_SECRET_TOKEN", isEnabled = true, isSecret = true });

        var createdVariable = await createVariableResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        Assert.NotNull(createdVariable);

        var variablesResponse = await client.GetAsync($"/api/environments/{environmentId}/variables");
        var variables = await variablesResponse.Content.ReadFromJsonAsync<List<VariableDto>>();

        Assert.NotNull(variables);
        Assert.Equal("********", variables![0].Value);
        Assert.True(variables[0].IsSecret);

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);
        Assert.Contains(remote.Requests[0].Headers, x => x.Key == "Authorization" && x.Value == "Bearer REAL_SECRET_TOKEN");
    }

    [Fact]
    public async Task Execute_StructuredApiKey_FromConfiguration_AddsHeader()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote, new Dictionary<string, string?>
        {
            ["Secrets:ApiKey"] = "config-key"
        });
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 3,
                sourceKey = "Secrets:ApiKey",
                apiKeyHeaderName = "X-Api-Key"
            });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);
        Assert.Equal("https://remote.test/posts", remote.Requests[0].Url);
        Assert.Contains(remote.Requests[0].Headers, x => x.Key == "X-Api-Key" && x.Value == "config-key");
    }

    [Fact]
    public async Task Execute_StructuredApiKeyQuery_FromSecretVariable_AppendsQueryAndDoesNotPersistGeneratedParameter()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "api_key",
                apiKeyLocation = 2
            });

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiKey", value = "abc123", isEnabled = true, isSecret = true });

        await client.PostAsJsonAsync(
            $"/api/operations/{operationId}/query-parameters",
            new { key = "page", value = "2", isEnabled = true });

        var variables = await (await client.GetAsync($"/api/environments/{environmentId}/variables"))
            .Content.ReadFromJsonAsync<List<VariableDto>>();

        Assert.NotNull(variables);
        Assert.Contains(variables!, x => x.Key == "ApiKey" && x.Value == "********" && x.IsSecret);

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);
        Assert.Equal("https://remote.test/posts?page=2&api_key=abc123", remote.Requests[0].Url);
        Assert.Empty(remote.Requests[0].Headers);

        var queryParameters = await client.GetFromJsonAsync<List<QueryParameterDto>>($"/api/operations/{operationId}/query-parameters");
        Assert.NotNull(queryParameters);
        Assert.Single(queryParameters!);
        Assert.Equal("page", queryParameters[0].Key);
    }

    [Fact]
    public async Task Execute_StructuredApiKeyQuery_SelectedEnvironmentDeterminesValue()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var developmentEnvironmentId = await CreateEnvironmentAsync(client, dataSourceId, name: "Development", baseUrl: "https://dev.remote.test");
        var productionEnvironmentId = await CreateEnvironmentAsync(client, dataSourceId, name: "Production", baseUrl: "https://prod.remote.test");
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "api_key",
                apiKeyLocation = 2
            });

        await client.PostAsJsonAsync($"/api/environments/{developmentEnvironmentId}/variables", new { key = "ApiKey", value = "dev-123", isEnabled = true, isSecret = true });
        await client.PostAsJsonAsync($"/api/environments/{productionEnvironmentId}/variables", new { key = "ApiKey", value = "prod-456", isEnabled = true, isSecret = true });

        await ExecuteOperationAsync(client, operationId, developmentEnvironmentId);
        await ExecuteOperationAsync(client, operationId, productionEnvironmentId);

        Assert.Equal(2, remote.Requests.Count);
        Assert.Equal("https://dev.remote.test/posts?api_key=dev-123", remote.Requests[0].Url);
        Assert.Equal("https://prod.remote.test/posts?api_key=prod-456", remote.Requests[1].Url);
    }

    [Fact]
    public async Task Execute_StructuredApiKeyQuery_UsesBuildFinalUrlEncodingExactlyOnce()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 2,
                valueSourceType = 2,
                sourceKey = "ApiKey",
                apiKeyHeaderName = "api key",
                apiKeyLocation = 2
            });

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiKey", value = "abc+123&x=y?/ spaces ü", isEnabled = true, isSecret = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);
        Assert.Equal(
            "https://remote.test/posts?api%20key=abc%2B123%26x%3Dy%3F%2F%20spaces%20%C3%BC",
            remote.Requests[0].Url);
    }

    [Fact]
    public async Task Execute_StructuredBasic_FromVariables_UsesSecretPassword_AndDoesNotPersistAuthorizationHeader()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiUsername", value = "dev-user", isEnabled = true, isSecret = false });

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiPassword", value = "dev-password", isEnabled = true, isSecret = true });

        var variablesResponse = await client.GetAsync($"/api/environments/{environmentId}/variables");
        var variables = await variablesResponse.Content.ReadFromJsonAsync<List<VariableDto>>();

        Assert.NotNull(variables);
        Assert.Contains(variables!, x => x.Key == "ApiPassword" && x.Value == "********" && x.IsSecret);

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);

        var authorizationHeader = remote.Requests[0].Headers.Single(x => x.Key == "Authorization").Value;
        Assert.StartsWith("Basic ", authorizationHeader, StringComparison.Ordinal);

        var encoded = authorizationHeader["Basic ".Length..];
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        Assert.Equal("dev-user:dev-password", decoded);

        var authResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/authentication");
        var auth = await authResponse.Content.ReadFromJsonAsync<DataSourceAuthenticationDto>();
        Assert.NotNull(auth);
        Assert.Null(auth!.ValueSourceType);
        Assert.Null(auth.SourceKey);
        Assert.Equal("ApiUsername", auth.UsernameSourceKey);
        Assert.Equal("ApiPassword", auth.PasswordSourceKey);

        var headersResponse = await client.GetAsync($"/api/data-sources/{dataSourceId}/headers");
        var headers = await headersResponse.Content.ReadFromJsonAsync<List<HeaderDto>>();
        Assert.NotNull(headers);
        Assert.Empty(headers!);
    }

    [Fact]
    public async Task Execute_StructuredBasic_SelectedEnvironmentDeterminesCredentials()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var developmentEnvironmentId = await CreateEnvironmentAsync(client, dataSourceId, name: "Development", baseUrl: "https://dev.remote.test");
        var productionEnvironmentId = await CreateEnvironmentAsync(client, dataSourceId, name: "Production", baseUrl: "https://prod.remote.test");
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        await client.PostAsJsonAsync($"/api/environments/{developmentEnvironmentId}/variables", new { key = "ApiUsername", value = "dev-user", isEnabled = true });
        await client.PostAsJsonAsync($"/api/environments/{developmentEnvironmentId}/variables", new { key = "ApiPassword", value = "dev-pass", isEnabled = true, isSecret = true });
        await client.PostAsJsonAsync($"/api/environments/{productionEnvironmentId}/variables", new { key = "ApiUsername", value = "prod-user", isEnabled = true });
        await client.PostAsJsonAsync($"/api/environments/{productionEnvironmentId}/variables", new { key = "ApiPassword", value = "prod-pass", isEnabled = true, isSecret = true });

        await ExecuteOperationAsync(client, operationId, developmentEnvironmentId);
        await ExecuteOperationAsync(client, operationId, productionEnvironmentId);

        Assert.Equal(2, remote.Requests.Count);

        var devHeader = remote.Requests[0].Headers.Single(x => x.Key == "Authorization").Value;
        var prodHeader = remote.Requests[1].Headers.Single(x => x.Key == "Authorization").Value;

        Assert.Equal("dev-user:dev-pass", DecodeBasicHeader(devHeader));
        Assert.Equal("prod-user:prod-pass", DecodeBasicHeader(prodHeader));
    }

    [Fact]
    public async Task Execute_StructuredBasic_MixedSources_UsesVariableUsername_AndConfigurationPassword()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote, new Dictionary<string, string?>
        {
            ["Secrets:HrPassword"] = "config-pass"
        });
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 3,
                passwordSourceKey = "Secrets:HrPassword"
            });

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "ApiUsername", value = "mixed-user", isEnabled = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);
        Assert.Equal("mixed-user:config-pass", DecodeBasicHeader(remote.Requests[0].Headers.Single(x => x.Key == "Authorization").Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("user:name")]
    public async Task Execute_StructuredBasic_InvalidUsername_ReturnsAuthenticationResolutionError(string username)
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiUsername", value = username, isEnabled = true });
        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiPassword", value = "valid-password", isEnabled = true, isSecret = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("AuthenticationResolutionError", result.ErrorType);
        Assert.Equal("Unable to use Basic authentication username source 'ApiUsername'.", result.ErrorMessage);
        Assert.DoesNotContain("valid-password", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Empty(remote.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Execute_StructuredBasic_EmptyOrWhitespacePassword_IsAllowed(string password)
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiUsername", value = "basic-user", isEnabled = true });
        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiPassword", value = password, isEnabled = true, isSecret = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);
        Assert.Equal($"basic-user:{password}", DecodeBasicHeader(remote.Requests[0].Headers.Single(x => x.Key == "Authorization").Value));
    }

    [Fact]
    public async Task Execute_StructuredBasic_MissingPassword_ReturnsAuthenticationResolutionError()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiUsername", value = "basic-user", isEnabled = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("AuthenticationResolutionError", result.ErrorType);
        Assert.Equal("Unable to resolve Basic authentication password source 'ApiPassword'.", result.ErrorMessage);
        Assert.Empty(remote.Requests);
    }

    [Fact]
    public async Task Execute_StructuredBasic_UsesUtf8ForUnicodeCredentials()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiUsername", value = "naïve", isEnabled = true });
        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiPassword", value = "päss", isEnabled = true, isSecret = true });

        await ExecuteOperationAsync(client, operationId, environmentId);

        var authorizationHeader = remote.Requests.Single().Headers.Single(x => x.Key == "Authorization").Value;
        var encoded = authorizationHeader["Basic ".Length..];
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("naïve:päss"));

        Assert.Equal(expected, encoded);
    }

    [Fact]
    public async Task Execute_StructuredAuthentication_BlankResolvedValue_ReturnsAuthenticationResolutionError()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote, new Dictionary<string, string?>
        {
            ["Secrets:BlankToken"] = "   "
        });
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 1,
                valueSourceType = 3,
                sourceKey = "Secrets:BlankToken",
                apiKeyHeaderName = (string?)null
            });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("AuthenticationResolutionError", result.ErrorType);
        Assert.Equal("Unable to resolve Bearer authentication source 'Secrets:BlankToken'.", result.ErrorMessage);
        Assert.Empty(remote.Requests);
    }

    [Fact]
    public async Task Execute_OperationAuthenticationModeNone_SuppressesStructuredAuthentication()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId, authenticationMode: 2);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 1,
                valueSourceType = 2,
                sourceKey = "AccessToken",
                apiKeyHeaderName = (string?)null
            });

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "AccessToken", value = "SHOULD_NOT_SEND", isEnabled = true, isSecret = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);
        Assert.DoesNotContain(remote.Requests[0].Headers, x => x.Key == "Authorization");
    }

    [Fact]
    public async Task Execute_OperationAuthenticationModeNone_SuppressesStructuredBasic_ButKeepsRawHeaders()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId, authenticationMode: 2);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 3,
                valueSourceType = (int?)null,
                sourceKey = (string?)null,
                apiKeyHeaderName = (string?)null,
                usernameSourceType = 2,
                usernameSourceKey = "ApiUsername",
                passwordSourceType = 2,
                passwordSourceKey = "ApiPassword"
            });

        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiUsername", value = "should-not-send", isEnabled = true });
        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiPassword", value = "should-not-send", isEnabled = true, isSecret = true });
        await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/headers",
            new { key = "X-Trace", valueSourceType = 1, value = "trace-1", sourceKey = (string?)null, isEnabled = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Single(remote.Requests);
        Assert.DoesNotContain(remote.Requests[0].Headers, x => x.Key == "Authorization");
        Assert.Contains(remote.Requests[0].Headers, x => x.Key == "X-Trace" && x.Value == "trace-1");
    }

    [Fact]
    public async Task Execute_DefensiveStructuredAuthConflict_ReturnsAuthenticationConfigurationError()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await SeedStructuredAuthenticationAsync(
            factory,
            new SeededAuthentication(dataSourceId, 1, 2, "AccessToken", null));

        await SeedRawDataSourceHeaderAsync(
            factory,
            dataSourceId,
            new SeededHeader("Authorization", 1, "Bearer raw", null, true));

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "AccessToken", value = "REAL_SECRET_TOKEN", isEnabled = true, isSecret = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("AuthenticationConfigurationError", result.ErrorType);
        Assert.Equal("Structured Bearer authentication conflicts with raw header 'Authorization'.", result.ErrorMessage);
        Assert.Empty(remote.Requests);
    }

    [Fact]
    public async Task Execute_DefensiveStructuredBasicConflict_ReturnsAuthenticationConfigurationError()
    {
        using var remote = new RemoteHttpStub();
        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await SeedStructuredAuthenticationAsync(
            factory,
            new SeededAuthentication(
                dataSourceId,
                3,
                null,
                null,
                null,
                UsernameSourceType: 2,
                UsernameSourceKey: "ApiUsername",
                PasswordSourceType: 2,
                PasswordSourceKey: "ApiPassword"));

        await SeedRawDataSourceHeaderAsync(
            factory,
            dataSourceId,
            new SeededHeader("authorization", 1, "Basic raw", null, true));

        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiUsername", value = "basic-user", isEnabled = true });
        await client.PostAsJsonAsync($"/api/environments/{environmentId}/variables", new { key = "ApiPassword", value = "basic-pass", isEnabled = true, isSecret = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.True(result!.HasExecutionError);
        Assert.Equal("AuthenticationConfigurationError", result.ErrorType);
        Assert.Equal("Structured Basic authentication conflicts with raw header 'Authorization'.", result.ErrorMessage);
        Assert.Empty(remote.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.Forbidden, 403)]
    public async Task Execute_StructuredBearer_Remote401And403_RemainNormalRemoteResponses(HttpStatusCode remoteStatusCode, int expectedStatusCode)
    {
        using var remote = new RemoteHttpStub
        {
            Responder = _ => Task.FromResult(new HttpResponseMessage(remoteStatusCode)
            {
                Content = new StringContent(remoteStatusCode.ToString(), Encoding.UTF8, "text/plain")
            })
        };

        using var factory = new SimpleApiTesterApiFactory(remote);
        using var client = factory.CreateApiClient();

        var dataSourceId = await CreateDataSourceAsync(client);
        var environmentId = await CreateEnvironmentAsync(client, dataSourceId);
        var operationId = await CreateOperationAsync(client, dataSourceId);

        await client.PutAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/authentication",
            new
            {
                authenticationType = 1,
                valueSourceType = 2,
                sourceKey = "AccessToken",
                apiKeyHeaderName = (string?)null
            });

        await client.PostAsJsonAsync(
            $"/api/environments/{environmentId}/variables",
            new { key = "AccessToken", value = "REAL_SECRET_TOKEN", isEnabled = true, isSecret = true });

        var response = await ExecuteOperationAsync(client, operationId, environmentId);
        var result = await response.Content.ReadFromJsonAsync<ExecuteOperationDto>();

        Assert.NotNull(result);
        Assert.False(result!.HasExecutionError);
        Assert.Equal(expectedStatusCode, result.StatusCode);
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

    private static async Task<Guid> CreateDataSourceAsync(HttpClient client, string? key = null, int? defaultTimeoutSeconds = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/data-sources",
            new
            {
                key = key ?? $"jsonplaceholder-{Guid.NewGuid():N}",
                defaultTimeoutSeconds
            });

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
        string? contentType = null,
        int authenticationMode = 1)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/data-sources/{dataSourceId}/operations",
            new
            {
                apiName,
                endpoint,
                methodType,
                body,
                contentType,
                authenticationMode
            });

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CreatedIdResponse>();
        return created!.Id;
    }

    private static Task<HttpResponseMessage> ExecuteOperationAsync(HttpClient client, Guid operationId, Guid environmentId)
        => client.PostAsync($"/api/operations/{operationId}/execute?environmentId={environmentId}", null);

    private static Task<HttpResponseMessage> TestConnectionAsync(HttpClient client, Guid dataSourceId, Guid environmentId)
        => client.PostAsync($"/api/data-sources/{dataSourceId}/test-connection?environmentId={environmentId}", null);

    private static async Task<JsonDocument> ReadProblemDocumentAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static async Task<JsonDocument> ReadProblemOrJsonDocumentAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static string DecodeBasicHeader(string authorizationHeader)
    {
        Assert.StartsWith("Basic ", authorizationHeader, StringComparison.Ordinal);
        return Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader["Basic ".Length..]));
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

    private static async Task SeedStructuredAuthenticationAsync(SimpleApiTesterApiFactory factory, SeededAuthentication authentication)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.DataSourceAuthentications.Add(new SimpleApiTester.Domain.Entities.DataSourceAuthentication
        {
            Id = Guid.NewGuid(),
            DataSourceId = authentication.DataSourceId,
            AuthenticationType = (SimpleApiTester.Domain.Enum.AuthenticationType)authentication.AuthenticationType,
            ValueSourceType = authentication.ValueSourceType is null
                ? null
                : (SimpleApiTester.Domain.Enum.HeaderValueSourceType)authentication.ValueSourceType.Value,
            SourceKey = authentication.SourceKey,
            ApiKeyHeaderName = authentication.ApiKeyHeaderName,
            ApiKeyLocation = authentication.ApiKeyLocation is null
                ? null
                : (SimpleApiTester.Domain.Enum.ApiKeyLocation)authentication.ApiKeyLocation.Value,
            UsernameSourceType = authentication.UsernameSourceType is null
                ? null
                : (SimpleApiTester.Domain.Enum.HeaderValueSourceType)authentication.UsernameSourceType.Value,
            UsernameSourceKey = authentication.UsernameSourceKey,
            PasswordSourceType = authentication.PasswordSourceType is null
                ? null
                : (SimpleApiTester.Domain.Enum.HeaderValueSourceType)authentication.PasswordSourceType.Value,
            PasswordSourceKey = authentication.PasswordSourceKey,
            OAuthTokenEndpoint = authentication.OAuthTokenEndpoint,
            OAuthClientIdSourceType = authentication.OAuthClientIdSourceType is null
                ? null
                : (SimpleApiTester.Domain.Enum.HeaderValueSourceType)authentication.OAuthClientIdSourceType.Value,
            OAuthClientIdSourceKey = authentication.OAuthClientIdSourceKey,
            OAuthClientSecretSourceType = authentication.OAuthClientSecretSourceType is null
                ? null
                : (SimpleApiTester.Domain.Enum.HeaderValueSourceType)authentication.OAuthClientSecretSourceType.Value,
            OAuthClientSecretSourceKey = authentication.OAuthClientSecretSourceKey,
            OAuthScope = authentication.OAuthScope
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRawDataSourceHeaderAsync(SimpleApiTesterApiFactory factory, Guid dataSourceId, SeededHeader header)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.Headers.Add(new SimpleApiTester.Domain.Entities.Header
        {
            Id = Guid.NewGuid(),
            DataSourceId = dataSourceId,
            Key = header.Key,
            ValueSourceType = (SimpleApiTester.Domain.Enum.HeaderValueSourceType)header.ValueSourceType,
            Value = header.Value,
            SourceKey = header.SourceKey,
            IsEnabled = header.IsEnabled
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRawQueryParameterAsync(
        SimpleApiTesterApiFactory factory,
        Guid operationId,
        string key,
        string? value,
        bool isEnabled)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.QueryParameters.Add(new SimpleApiTester.Domain.Entities.QueryParameter
        {
            Id = Guid.NewGuid(),
            OperationId = operationId,
            Key = key,
            Value = value,
            IsEnabled = isEnabled
        });

        await dbContext.SaveChangesAsync();
    }

    private sealed record CreatedIdResponse(Guid Id);

    private sealed record DataSourceDto(Guid Id, string Key, bool IsActive, int? DefaultTimeoutSeconds);

    private sealed record DataSourceEnvironmentDto(Guid Id, Guid DataSourceId, string Name, string BaseUrl, bool IsActive);

    private sealed record OperationDto(
        Guid Id,
        Guid DataSourceId,
        string ApiName,
        string Endpoint,
        int MethodType,
        string? Body,
        string? ContentType,
        int AuthenticationMode);

    private sealed record DataSourceAuthenticationDto(
        Guid Id,
        Guid DataSourceId,
        int AuthenticationType,
        int? ValueSourceType,
        string? SourceKey,
        string? ApiKeyHeaderName,
        int? ApiKeyLocation,
        int? UsernameSourceType,
        string? UsernameSourceKey,
        int? PasswordSourceType,
        string? PasswordSourceKey,
        string? OAuthTokenEndpoint,
        int? OAuthClientIdSourceType,
        string? OAuthClientIdSourceKey,
        int? OAuthClientSecretSourceType,
        string? OAuthClientSecretSourceKey,
        string? OAuthScope);

    private sealed record QueryParameterDto(Guid Id, Guid OperationId, string Key, string? Value, bool IsEnabled);

    private sealed record VariableDto(Guid Id, Guid DataSourceEnvironmentId, string Key, string? Value, bool IsEnabled, bool IsSecret);

    private sealed record StoredVariableState(string Key, string? Value, bool IsEnabled, bool IsSecret);

    private sealed record SeededAuthentication(
        Guid DataSourceId,
        int AuthenticationType,
        int? ValueSourceType,
        string? SourceKey,
        string? ApiKeyHeaderName,
        int? ApiKeyLocation = null,
        int? UsernameSourceType = null,
        string? UsernameSourceKey = null,
        int? PasswordSourceType = null,
        string? PasswordSourceKey = null,
        string? OAuthTokenEndpoint = null,
        int? OAuthClientIdSourceType = null,
        string? OAuthClientIdSourceKey = null,
        int? OAuthClientSecretSourceType = null,
        string? OAuthClientSecretSourceKey = null,
        string? OAuthScope = null);

    private sealed record SeededHeader(
        string Key,
        int ValueSourceType,
        string? Value,
        string? SourceKey,
        bool IsEnabled);

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

    private sealed record TestDataSourceConnectionDto(
        bool IsReachable,
        int? StatusCode,
        bool? IsSuccessStatusCode,
        long DurationMilliseconds,
        string? ContentType,
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

    private static IReadOnlyDictionary<string, string> ParseFormUrlEncoded(string body)
        => body.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                part => Uri.UnescapeDataString(part[0].Replace("+", " ")),
                part => part.Length > 1
                    ? Uri.UnescapeDataString(part[1].Replace("+", " "))
                    : string.Empty,
                StringComparer.Ordinal);
}
