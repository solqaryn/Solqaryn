using System.Text.Json;
using Solqaryn.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API;

public class ExceptionHandlingConcurrencyTests
{
    [Fact]
    public async Task InvokeAsync_DbUpdateConcurrencyException_Responde409ProblemDetails()
    {
        RequestDelegate next = _ => throw new DbUpdateConcurrencyException("conflict");
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(next, logger.Object);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-n67d"
        };
        context.Request.Path = "/empresa-configuracion/tenant/23";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status409Conflict, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Conflicto de concurrencia", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("trace-n67d", document.RootElement.GetProperty("traceId").GetString());
    }
}
