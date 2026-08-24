using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleApiTester.Application.Abstractions.Authentication;

namespace SimpleApiTester.Infrastructure.Services;

internal sealed class OAuthTokenClient : IOAuthTokenClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;

    public OAuthTokenClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OAuthTokenClientResult> RequestClientCredentialsTokenAsync(
        OAuthTokenRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCancellationTokenSource.CancelAfter(timeout);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(CreateFormValues(request))
            };

            using var response = await _httpClientFactory
                .CreateClient("OAuthTokenClient")
                .SendAsync(httpRequest, linkedCancellationTokenSource.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new OAuthTokenClientResult(
                    null,
                    $"OAuth token request returned HTTP {(int)response.StatusCode}.");
            }

            OAuthTokenResponsePayload? payload;

            try
            {
                payload = await response.Content.ReadFromJsonAsync<OAuthTokenResponsePayload>(
                    JsonOptions,
                    linkedCancellationTokenSource.Token);
            }
            catch (JsonException)
            {
                return new OAuthTokenClientResult(null, "OAuth token response was not valid JSON.");
            }
            catch (NotSupportedException)
            {
                return new OAuthTokenClientResult(null, "OAuth token response was not valid JSON.");
            }

            if (payload is null)
            {
                return new OAuthTokenClientResult(null, "OAuth token response was not valid JSON.");
            }

            if (string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                return new OAuthTokenClientResult(null, "OAuth token response did not contain access_token.");
            }

            if (!string.IsNullOrWhiteSpace(payload.TokenType)
                && !string.Equals(payload.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase))
            {
                return new OAuthTokenClientResult(null, "OAuth token response contained unsupported token_type.");
            }

            return new OAuthTokenClientResult(payload.AccessToken, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new OAuthTokenClientResult(null, "OAuth token request timed out.");
        }
        catch (HttpRequestException)
        {
            return new OAuthTokenClientResult(null, "OAuth token request failed.");
        }
    }

    private static IReadOnlyCollection<KeyValuePair<string, string>> CreateFormValues(OAuthTokenRequest request)
    {
        var values = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", request.ClientId),
            new("client_secret", request.ClientSecret)
        };

        if (!string.IsNullOrWhiteSpace(request.Scope))
        {
            values.Add(new KeyValuePair<string, string>("scope", request.Scope));
        }

        return values;
    }

    private sealed record OAuthTokenResponsePayload(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("expires_in")] int? ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope);
}
