using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Application.Operations.Commands.ExecuteOperation;
using SimpleApiTester.Domain.Enum;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SimpleApiTester.Infrastructure.Services;

internal sealed class OperationRequestExecutor : IOperationRequestExecutor
{
    private const string ClientName = "OperationExecutor";
    private static readonly MediaTypeHeaderValue JsonMediaType = new("application/json")
    {
        CharSet = "utf-8"
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public OperationRequestExecutor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ExecuteOperationResponse> ExecuteAsync(
        OperationHttpRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var requestMessage = new HttpRequestMessage(
                CreateHttpMethod(request.MethodType),
                request.Url);

            if (ShouldAttachBody(request.MethodType) && !string.IsNullOrWhiteSpace(request.Body))
            {
                requestMessage.Content = new StringContent(request.Body, Encoding.UTF8);
                requestMessage.Content.Headers.ContentType = JsonMediaType;
            }

            var httpClient = _httpClientFactory.CreateClient(ClientName);

            using var response = await httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var responseBody = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken);

            stopwatch.Stop();

            return new ExecuteOperationResponse(
                StatusCode: (int)response.StatusCode,
                IsSuccessStatusCode: response.IsSuccessStatusCode,
                ResponseBody: responseBody,
                ContentType: response.Content?.Headers.ContentType?.ToString(),
                DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                HasExecutionError: false,
                ErrorType: null,
                ErrorMessage: null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            return new ExecuteOperationResponse(
                StatusCode: null,
                IsSuccessStatusCode: null,
                ResponseBody: null,
                ContentType: null,
                DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                HasExecutionError: true,
                ErrorType: "Timeout",
                ErrorMessage: "The HTTP request timed out.");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();

            return new ExecuteOperationResponse(
                StatusCode: null,
                IsSuccessStatusCode: null,
                ResponseBody: null,
                ContentType: null,
                DurationMilliseconds: stopwatch.ElapsedMilliseconds,
                HasExecutionError: true,
                ErrorType: "HttpRequestError",
                ErrorMessage: ex.Message);
        }
    }

    private static HttpMethod CreateHttpMethod(HttpMethodType methodType) => methodType switch
    {
        HttpMethodType.Get => HttpMethod.Get,
        HttpMethodType.Post => HttpMethod.Post,
        HttpMethodType.Put => HttpMethod.Put,
        HttpMethodType.Patch => HttpMethod.Patch,
        HttpMethodType.Delete => HttpMethod.Delete,
        _ => throw new InvalidOperationException($"Unsupported HTTP method type '{methodType}'.")
    };

    private static bool ShouldAttachBody(HttpMethodType methodType) => methodType switch
    {
        HttpMethodType.Post => true,
        HttpMethodType.Put => true,
        HttpMethodType.Patch => true,
        _ => false
    };
}
