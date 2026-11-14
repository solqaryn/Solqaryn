using Solqaryn.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Solqaryn.Tests;

public class M13ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task RequestAborted_NoSeConvierteEnError500()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        RequestDelegate next = _ => throw new OperationCanceledException(cancellation.Token);
        var middleware = new ExceptionHandlingMiddleware(
            next,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext
        {
            RequestAborted = cancellation.Token
        };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.NotEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }
}