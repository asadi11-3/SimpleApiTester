using FluentValidation;
using Microsoft.AspNetCore.Http;
using SimpleApiTester.API.Infrastructure;
using System.Text.Json;

namespace SimpleApiTester.Tests.API;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ForUnexpectedException_ReturnsGeneric500ProblemDetails()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-123";
        httpContext.Request.Path = "/api/test";
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            httpContext,
            new Exception("sensitive connection details"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);

        Assert.Equal(500, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "An unexpected error occurred while processing the request.",
            document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("trace-123", document.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_ForValidationException_ReturnsValidationErrors()
    {
        var handler = new GlobalExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/test";
        httpContext.Response.Body = new MemoryStream();

        var exception = new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure("Key", "Key is required."),
                new FluentValidation.Results.ValidationFailure("Key", "Key is too long.")
            ]);

        var handled = await handler.TryHandleAsync(
            httpContext,
            exception,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);

        Assert.Equal("Validation failed", document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("Key", out var keyErrors));
        Assert.Equal(2, keyErrors.GetArrayLength());
    }
}
