using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Common;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>
/// V5 C00-01.M05.02: real in-memory HTTP pipeline and JSON contract checks.
/// The TestServer uses the production ExceptionHandlingMiddleware; no database
/// or external service is needed for these API/DTO response assertions.
/// </summary>
public sealed class V5C00M0502ApiDtoContractTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ExitoHttp200_ConservaContratoApiResponseConDatos()
    {
        using var server = CreateServer(async context =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                ApiResponse<string>.Ok("confirmado", "operacion exitosa"), WebJson));
        });
        using var client = server.CreateClient();
        using var response = await client.GetAsync("/api/v5/contrato");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("confirmado", json.RootElement.GetProperty("data").GetString());
        Assert.Equal("operacion exitosa", json.RootElement.GetProperty("message").GetString());
        Assert.Empty(json.RootElement.GetProperty("errors").EnumerateArray());
    }

    [Theory]
    [InlineData("password=secret-v5-should-not-appear")]
    [InlineData("tenant-B; sql=SELECT * FROM Usuarios")]
    public async Task ArgumentoInvalidoHttp400_NoExponeDetallePrivado(string detail)
    {
        using var server = CreateServer(_ => throw new ArgumentException(detail));
        using var client = server.CreateClient();
        using var response = await client.GetAsync("/api/v5/contrato");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Solicitud inv\u00e1lida",
            json.RootElement.GetProperty("title").GetString());
        Assert.Equal("/api/v5/contrato", json.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(detail, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConcurrenciaHttp409_UsaProblemDetailsGenericoSinDetalleDeTenant()
    {
        const string secret = "tenant-B-secret-mismatch";
        using var server = CreateServer(_ => throw new DbUpdateConcurrencyException(secret));
        using var client = server.CreateClient();
        using var response = await client.GetAsync("/api/v5/contrato");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(409, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Conflicto de concurrencia", json.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErrorNoControladoHttp500_ConservaEnvelopeSinDetalleTecnico()
    {
        const string secret = "password=internal-unhandled-exception-secret";
        using var server = CreateServer(_ => throw new InvalidOperationException(secret));
        using var client = server.CreateClient();
        using var response = await client.GetAsync("/api/v5/contrato");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(body);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Null(json.RootElement.GetProperty("data").GetString());
        Assert.Contains("error interno", json.RootElement.GetProperty("message").GetString()!, StringComparison.Ordinal);
        Assert.Empty(json.RootElement.GetProperty("errors").EnumerateArray());
        Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
    }

    private static TestServer CreateServer(RequestDelegate next) => new(
        new WebHostBuilder()
            .ConfigureServices(services => services.AddLogging())
            .Configure(app =>
            {
                app.UseMiddleware<ExceptionHandlingMiddleware>();
                app.Run(next);
            }));

}
