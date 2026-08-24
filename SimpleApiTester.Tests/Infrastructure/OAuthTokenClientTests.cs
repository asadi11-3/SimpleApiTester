using System.Net;
using System.Text;
using SimpleApiTester.Application.Abstractions.Authentication;
using SimpleApiTester.Infrastructure.Services;

namespace SimpleApiTester.Tests.Infrastructure;

public sealed class OAuthTokenClientTests
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task RequestClientCredentialsTokenAsync_WhenTokenResponseJsonIsMalformed_ReturnsSafeFailure()
    {
        var context = new RequestCaptureContext
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":}", Encoding.UTF8, "application/json")
            }
        };
        var client = CreateClient(context);

        var result = await client.RequestClientCredentialsTokenAsync(
            new OAuthTokenRequest(
                "https://identity.example.com/oauth/token",
                "fake-client",
                "fake-secret",
                "scope.read"),
            RequestTimeout,
            CancellationToken.None);

        Assert.Null(result.AccessToken);
        Assert.Equal("OAuth token response was not valid JSON.", result.ErrorMessage);
        Assert.DoesNotContain("fake-secret", result.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("access_token", result.ErrorMessage, StringComparison.Ordinal);
        Assert.NotNull(context.Request);
        Assert.Equal(HttpMethod.Post, context.Request!.Method);
    }

    [Fact]
    public async Task RequestClientCredentialsTokenAsync_WhenTokenTypeIsUnsupported_ReturnsSafeFailure()
    {
        var context = new RequestCaptureContext
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"token-123\",\"token_type\":\"MAC\"}", Encoding.UTF8, "application/json")
            }
        };
        var client = CreateClient(context);

        var result = await client.RequestClientCredentialsTokenAsync(
            new OAuthTokenRequest(
                "https://identity.example.com/oauth/token",
                "fake-client",
                "fake-secret",
                null),
            RequestTimeout,
            CancellationToken.None);

        Assert.Null(result.AccessToken);
        Assert.Equal("OAuth token response contained unsupported token_type.", result.ErrorMessage);
        Assert.DoesNotContain("token-123", result.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("fake-secret", result.ErrorMessage, StringComparison.Ordinal);
    }

    private static OAuthTokenClient CreateClient(RequestCaptureContext context)
        => new(new FakeHttpClientFactory(context));

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly RequestCaptureContext _context;

        public FakeHttpClientFactory(RequestCaptureContext context)
        {
            _context = context;
        }

        public HttpClient CreateClient(string name)
            => new(new CaptureHandler(_context));
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly RequestCaptureContext _context;

        public CaptureHandler(RequestCaptureContext context)
        {
            _context = context;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _context.Request = new HttpRequestMessage(request.Method, request.RequestUri);

            if (request.Content is not null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                _context.Request.Content = new StringContent(body, Encoding.UTF8);
                _context.Request.Content.Headers.ContentType = request.Content.Headers.ContentType;
            }

            return _context.Response;
        }
    }

    private sealed class RequestCaptureContext
    {
        public HttpRequestMessage? Request { get; set; }

        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }
}
