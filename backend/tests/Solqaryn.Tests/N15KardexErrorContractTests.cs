using System.Text.Json;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Common;
using Solqaryn.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexErrorContractTests
{
    [Fact]
    public async Task BusinessRuleException_Se_Expone_Como_BadRequest_Sin_Filtrar_Detalle_Tecnico()
    {
        const string mensaje = "La fecha inicial del Kardex no puede ser posterior a la fecha final.";
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new BusinessRuleException(mensaje),
            logger.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<object>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(envelope);
        Assert.False(envelope!.Success);
        Assert.Equal(mensaje, envelope.Message);
        Assert.Empty(envelope.Errors);
    }
}
