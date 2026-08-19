using SimpleApiTester.Application.Abstractions.Http;
using SimpleApiTester.Infrastructure.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SimpleApiTester.Tests.Infrastructure;

public sealed class OperationRequestExecutorTests
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
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
            RequestTimeout,
            CancellationToken.None);

        Assert.NotNull(context.Request);
        Assert.Equal("Bearer token", context.Request!.Headers.GetValues("Authorization").Single());
        Assert.Equal("trace-1", context.Request.Headers.GetValues("X-Trace").Single());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRemoteRespondsWith504_ReturnsNormalRemoteResult()
    {
        var context = CreateContext();
        context.Response = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
        {
            Content = new StringContent("gateway timeout")
        };
        var executor = CreateExecutor(context);

        var result = await executor.ExecuteAsync(
            new OperationHttpRequest("https://example.com/posts", Domain.Enum.HttpMethodType.Get, null, null),
            RequestTimeout,
            CancellationToken.None);

        Assert.False(result.HasExecutionError);
        Assert.Equal(504, result.StatusCode);
        Assert.False(result.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenResponseBodyStallsBeyondTimeout_ReturnsTimeout()
    {
        var context = CreateContext();
        context.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new SlowReadableContent()
        };
        var executor = CreateExecutor(context);

        var result = await executor.ExecuteAsync(
            new OperationHttpRequest("https://example.com/posts", Domain.Enum.HttpMethodType.Get, null, null),
            TimeSpan.FromMilliseconds(20),
            CancellationToken.None);

        Assert.True(result.HasExecutionError);
        Assert.Equal("Timeout", result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCallerCancels_PropagatesCancellation()
    {
        var context = CreateContext();
        context.ResponseFactory = async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };
        var executor = CreateExecutor(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            executor.ExecuteAsync(
                new OperationHttpRequest("https://example.com/posts", Domain.Enum.HttpMethodType.Get, null, null),
                RequestTimeout,
                cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenDifferentTimeoutsRunConcurrently_DoNotInterfere()
    {
        var sharedHttpClient = new HttpClient(new DelayedHandler())
        {
            Timeout = Timeout.InfiniteTimeSpan,
            BaseAddress = new Uri("https://example.com")
        };
        var executor = new OperationRequestExecutor(new SharedHttpClientFactory(sharedHttpClient));

        var shortTask = executor.ExecuteAsync(
            new OperationHttpRequest("https://example.com/slow", Domain.Enum.HttpMethodType.Get, null, null),
            TimeSpan.FromMilliseconds(20),
            CancellationToken.None);
        var longTask = executor.ExecuteAsync(
            new OperationHttpRequest("https://example.com/fast", Domain.Enum.HttpMethodType.Get, null, null),
            TimeSpan.FromMilliseconds(200),
            CancellationToken.None);

        var results = await Task.WhenAll(shortTask, longTask);

        Assert.True(results[0].HasExecutionError);
        Assert.Equal("Timeout", results[0].ErrorType);
        Assert.False(results[1].HasExecutionError);
        Assert.Equal(200, results[1].StatusCode);
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

    private sealed class SharedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public SharedHttpClientFactory(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public HttpClient CreateClient(string name) => _httpClient;
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

            return await _context.ResponseFactory(request, cancellationToken);
        }
    }

    private sealed class DelayedHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var delay = request.RequestUri!.AbsoluteUri.Contains("slow", StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromMilliseconds(100)
                : TimeSpan.FromMilliseconds(10);

            await Task.Delay(delay, cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            };
        }
    }

    private sealed class SlowReadableContent : HttpContent
    {
        public SlowReadableContent()
        {
            Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return SerializeToStreamAsync(stream, context, CancellationToken.None);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            Stream stream = new SlowReadStream();
            return Task.FromResult(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    private sealed class SlowReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }

    private sealed class RequestCaptureContext
    {
        public HttpRequestMessage? Request { get; set; }

        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        };

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> ResponseFactory { get; set; }
            = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });

        public RequestCaptureContext()
        {
            ResponseFactory = (_, _) => Task.FromResult(Response);
        }
    }
}
