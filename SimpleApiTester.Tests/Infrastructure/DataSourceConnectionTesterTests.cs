using SimpleApiTester.Application.DataSources;
using SimpleApiTester.Infrastructure;
using SimpleApiTester.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;

namespace SimpleApiTester.Tests.Infrastructure;

public sealed class DataSourceConnectionTesterTests
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task TestConnectionAsync_WhenRemoteResponds_ReturnsReachable(HttpStatusCode statusCode, bool isSuccess)
    {
        var context = new RequestCaptureContext
        {
            ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
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

        var result = await tester.TestConnectionAsync("https://example.com/root", RequestTimeout, CancellationToken.None);

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
            ResponseFactory = (_, _) => throw new OperationCanceledException()
        };

        var tester = CreateTester(context);

        var result = await tester.TestConnectionAsync("https://example.com/root", RequestTimeout, CancellationToken.None);

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
            ResponseFactory = (_, _) => throw new HttpRequestException("socket failure should not leak")
        };

        var tester = CreateTester(context);

        var result = await tester.TestConnectionAsync("https://example.com/root", RequestTimeout, CancellationToken.None);

        Assert.False(result.IsReachable);
        Assert.Null(result.StatusCode);
        Assert.Equal("HttpRequestError", result.ErrorType);
        Assert.Equal("Unable to connect to target.", result.ErrorMessage);
        Assert.DoesNotContain("socket failure", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenCallerCancels_PropagatesCancellation()
    {
        var context = new RequestCaptureContext
        {
            ResponseFactory = async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        };

        var tester = CreateTester(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            tester.TestConnectionAsync("https://example.com/root", RequestTimeout, cts.Token));
    }

    [Fact]
    public async Task TestConnectionAsync_DoesNotMutateSharedHttpClientTimeout()
    {
        var sharedHttpClient = new HttpClient(new DelayedHandler())
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        var tester = new DataSourceConnectionTester(new SharedHttpClientFactory(sharedHttpClient));

        await tester.TestConnectionAsync("https://example.com/fast", TimeSpan.FromMilliseconds(20), CancellationToken.None);
        await tester.TestConnectionAsync("https://example.com/fast", TimeSpan.FromMilliseconds(200), CancellationToken.None);

        Assert.Equal(Timeout.InfiniteTimeSpan, sharedHttpClient.Timeout);
    }

    [Fact]
    public void AddInfrastructure_ConfiguresNamedHttpClientsWithInfiniteTimeout()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=SimpleApiTesterTests;Trusted_Connection=True;TrustServerCertificate=True"
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        Assert.Equal(Timeout.InfiniteTimeSpan, factory.CreateClient("OperationExecutor").Timeout);
        Assert.Equal(Timeout.InfiniteTimeSpan, factory.CreateClient("ConnectionTester").Timeout);
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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _context.Request = request;
            return _context.ResponseFactory(request, cancellationToken);
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

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class RequestCaptureContext
    {
        public HttpRequestMessage? Request { get; set; }

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> ResponseFactory { get; set; }
            = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
