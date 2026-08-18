using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Infrastructure.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SimpleApiTester.Tests.Infrastructure;

public sealed class OperationRequestExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_Post_WithJsonBody_UsesConfiguredJsonContentType()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Post,
                "{\"title\":\"SimpleApiTester\"}",
                "application/json"),
            CancellationToken.None);

        Assert.NotNull(context.Request);
        Assert.Equal(HttpMethod.Post, context.Request!.Method);
        Assert.NotNull(context.Request.Content);
        Assert.Equal("application/json; charset=utf-8", context.Request.Content!.Headers.ContentType!.ToString());
        Assert.Equal("{\"title\":\"SimpleApiTester\"}", await context.Request.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExecuteAsync_Post_WithPlainTextBody_UsesPlainTextContentType()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Post,
                "Hello from SimpleApiTester",
                "text/plain"),
            CancellationToken.None);

        Assert.Equal("text/plain; charset=utf-8", context.Request!.Content!.Headers.ContentType!.ToString());
        Assert.Equal("Hello from SimpleApiTester", await context.Request.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExecuteAsync_Post_WithXmlBody_SendsRawXmlUnchanged()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);
        const string body = "<user><name>John</name></user>";

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Post,
                body,
                "application/xml"),
            CancellationToken.None);

        Assert.Equal("application/xml; charset=utf-8", context.Request!.Content!.Headers.ContentType!.ToString());
        Assert.Equal(body, await context.Request.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WithUnicodeBody_UsesUtf8AndPreservesBody()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);
        const string body = "{\"message\":\"مرحبا\"}";

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Post,
                body,
                "application/json"),
            CancellationToken.None);

        Assert.Equal("application/json; charset=utf-8", context.Request!.Content!.Headers.ContentType!.ToString());
        Assert.Equal(body, await context.Request.Content.ReadAsStringAsync());
        Assert.Equal(Encoding.UTF8.WebName, context.Request.Content.Headers.ContentType!.CharSet);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBodyIsNull_DoesNotAttachContent()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Post,
                null,
                null),
            CancellationToken.None);

        Assert.Null(context.Request!.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBodyIsEmptyString_AttachesExplicitEmptyContent()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Post,
                string.Empty,
                "text/plain"),
            CancellationToken.None);

        Assert.NotNull(context.Request!.Content);
        Assert.Equal(string.Empty, await context.Request.Content!.ReadAsStringAsync());
        Assert.Equal("text/plain; charset=utf-8", context.Request.Content.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenContentTypeIsNull_UsesJsonFallback()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Post,
                "{\"name\":\"John\"}",
                null),
            CancellationToken.None);

        Assert.Equal("application/json; charset=utf-8", context.Request!.Content!.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenContentTypeContainsCharset_DoesNotDuplicateCharset()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Post,
                "{\"name\":\"John\"}",
                "application/json; charset=utf-8"),
            CancellationToken.None);

        Assert.Equal("application/json; charset=utf-8", context.Request!.Content!.Headers.ContentType!.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_Get_IgnoresStoredBody()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Get,
                "{\"ignored\":true}",
                "application/json"),
            CancellationToken.None);

        Assert.Equal(HttpMethod.Get, context.Request!.Method);
        Assert.Null(context.Request.Content);
    }

    [Theory]
    [InlineData(Domain.Enum.HttpMethodType.Post)]
    [InlineData(Domain.Enum.HttpMethodType.Put)]
    [InlineData(Domain.Enum.HttpMethodType.Patch)]
    public async Task ExecuteAsync_MethodsThatSupportBody_AttachContent(Domain.Enum.HttpMethodType methodType)
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                methodType,
                "payload",
                "text/plain"),
            CancellationToken.None);

        Assert.NotNull(context.Request!.Content);
        Assert.Equal("payload", await context.Request.Content!.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExecuteAsync_AppliesResolvedRequestHeaders()
    {
        var context = CreateContext();
        var executor = CreateExecutor(context);

        await executor.ExecuteAsync(
            new OperationHttpRequest(
                "https://example.com/posts",
                Domain.Enum.HttpMethodType.Get,
                null,
                null,
                [
                    new ResolvedRequestHeader("Authorization", "Bearer token"),
                    new ResolvedRequestHeader("X-Trace", "trace-1")
                ]),
            CancellationToken.None);

        Assert.NotNull(context.Request);
        Assert.Equal("Bearer token", context.Request!.Headers.GetValues("Authorization").Single());
        Assert.Equal("trace-1", context.Request.Headers.GetValues("X-Trace").Single());
    }

    private static OperationRequestExecutor CreateExecutor(RequestCaptureContext context)
        => new(new FakeHttpClientFactory(context));

    private static RequestCaptureContext CreateContext()
        => new();

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly RequestCaptureContext _context;

        public FakeHttpClientFactory(RequestCaptureContext context)
        {
            _context = context;
        }

        public HttpClient CreateClient(string name)
            => new(new CaptureHandler(_context))
            {
                BaseAddress = new Uri("https://example.com")
            };
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

            foreach (var header in request.Headers)
            {
                _context.Request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                var content = new StringContent(body, Encoding.UTF8);

                if (request.Content.Headers.ContentType is not null)
                {
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.Content.Headers.ContentType.ToString());
                }

                _context.Request.Content = content;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        }
    }

    private sealed class RequestCaptureContext
    {
        public HttpRequestMessage? Request { get; set; }
    }
}
