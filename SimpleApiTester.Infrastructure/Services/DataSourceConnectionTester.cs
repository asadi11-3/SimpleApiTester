using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.DataSources;
using System.Diagnostics;

namespace SimpleApiTester.Infrastructure.Services;

internal sealed class DataSourceConnectionTester : IDataSourceConnectionTester
{
    private const string ClientName = "ConnectionTester";

    private readonly IHttpClientFactory _httpClientFactory;

    public DataSourceConnectionTester(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TestDataSourceConnectionResponse> TestConnectionAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            var httpClient = _httpClientFactory.CreateClient(ClientName);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            stopwatch.Stop();

            return new TestDataSourceConnectionResponse(
                IsReachable: true,
                StatusCode: (int)response.StatusCode,
                IsSuccessStatusCode: response.IsSuccessStatusCode,
                DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                ContentType: response.Content?.Headers.ContentType?.ToString(),
                ErrorType: null,
                ErrorMessage: null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            return new TestDataSourceConnectionResponse(
                IsReachable: false,
                StatusCode: null,
                IsSuccessStatusCode: null,
                DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                ContentType: null,
                ErrorType: "Timeout",
                ErrorMessage: "The HTTP request timed out.");
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();

            return new TestDataSourceConnectionResponse(
                IsReachable: false,
                StatusCode: null,
                IsSuccessStatusCode: null,
                DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                ContentType: null,
                ErrorType: "HttpRequestError",
                ErrorMessage: "Unable to connect to target.");
        }
    }
}
