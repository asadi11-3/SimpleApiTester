using SimpleApiTester.Application.DataSources;
using SimpleApiTester.Infrastructure.Services;
using System.Net;
using System.Net.Http.Headers;

namespace SimpleApiTester.Tests.Infrastructure;

public sealed class DataSourceConnectionTesterTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task TestConnectionAsync_WhenRemoteResponds_ReturnsReachable(HttpStatusCode statusCode, bool isSuccess)
    {
        var context = new RequestCaptureContext
        {
            ResponseFactory = _ => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(string.Empty)
                {
                    Headers =
                    {
                        ContentType = MediaTypeHeaderValue.Parse("application/json")
                    }
                }
            })
        };

        var tester = CreateTester(context);

        var result = await tester.TestConnectionAsync("https://example.com/root", CancellationToken.None);

        Assert.True(result.IsReachable);
        Assert.Equal((int)statusCode, result.StatusCode);
        Assert.Equal(isSuccess, result.IsSuccessStatusCode);
        Assert.Equal("application/json", result.ContentType);
        Assert.Null(result.ErrorType);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(context.Request);
        Assert.Equal(HttpMethod.Get, context.Request!.Method);
        Assert.Equal("https://example.com/root", context.Request.RequestUri!.AbsoluteUri);
        Assert.Null(context.Request.Content);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenTimeoutOccurs_ReturnsTimeoutResult()
    {
        var context = new RequestCaptureContext
        {
            ResponseFactory = _ => throw new OperationCanceledException()
        };

        var tester = CreateTester(context);

        var result = await tester.TestConnectionAsync("https://example.com/root", CancellationToken.None);

        Assert.False(result.IsReachable);
        Assert.Null(result.StatusCode);
        Assert.Equal("Timeout", result.ErrorType);
        Assert.Equal("The HTTP request timed out.", result.ErrorMessage);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenHttpRequestExceptionOccurs_ReturnsSafeHttpRequestError()
    {
        var context = new RequestCaptureContext
        {
            ResponseFactory = _ => throw new HttpRequestException("socket failure should not leak")
        };

        var tester = CreateTester(context);

        var result = await tester.TestConnectionAsync("https://example.com/root", CancellationToken.None);

        Assert.False(result.IsReachable);
        Assert.Null(result.StatusCode);
        Assert.Equal("HttpRequestError", result.ErrorType);
        Assert.Equal("Unable to connect to target.", result.ErrorMessage);
        Assert.DoesNotContain("socket failure", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static DataSourceConnectionTester CreateTester(RequestCaptureContext context)
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _context.Request = request;
            return _context.ResponseFactory(request);
        }
    }

    private sealed class RequestCaptureContext
    {
        public HttpRequestMessage? Request { get; set; }

        public Func<HttpRequestMessage, Task<HttpResponseMessage>> ResponseFactory { get; set; }
            = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
