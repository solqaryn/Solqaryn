using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Solqaryn.API.Middleware;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class C00M0306SafeErrorsTests
{
    [Theory]
    [InlineData("password=secret-1234-do-not-disclose")]
    [InlineData("empresa=tenant-B; internal_sql=SELECT * FROM Usuarios")]
    public async Task ArgumentException_Devuelve400SeguroSinFiltrarDatosEnRespuestaNiLogs(string privateDetail)
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        RequestDelegate next = _ => throw new ArgumentException(privateDetail);
        var middleware = new ExceptionHandlingMiddleware(next, logger.Object);
        var context = new DefaultHttpContext { TraceIdentifier = "trace-C00-M0306" };
        context.Request.Path = "/api/ordenes-compra";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Solicitud inválida", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("La solicitud contiene un parámetro inválido.", document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("trace-C00-M0306", document.RootElement.GetProperty("traceId").GetString());
        Assert.Equal("/api/ordenes-compra", document.RootElement.GetProperty("instance").GetString());
        Assert.DoesNotContain(privateDetail, document.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.NotEmpty(logger.Invocations);
        Assert.DoesNotContain(privateDetail,
            string.Join(" ", logger.Invocations.SelectMany(call => call.Arguments.Select(arg => arg?.ToString()))),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestSinError_ContinuaSinCambiarEstadoNiEmitirLog()
    {
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(_ => Task.CompletedTask, logger.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Empty(logger.Invocations);
    }
}
