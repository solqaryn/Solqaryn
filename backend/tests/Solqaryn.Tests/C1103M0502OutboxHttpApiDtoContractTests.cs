using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C11-03.M05.02: actual MVC outbox route, JWT Bearer middleware, HTTP, and safe errors.
/// Permission and application service are test doubles; EF is InMemory. No MySQL certification.
/// </summary>
public sealed class C1103M0502OutboxHttpApiDtoContractTests
{
    private const string Issuer = "c1103-m0502-test-issuer";
    private const string Audience = "c1103-m0502-test-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c1103-m0502-isolated-valid-test-jwt-key-2026"));
    private static readonly SymmetricSecurityKey Forged = new(
        Encoding.UTF8.GetBytes("c1103-m0502-isolated-forged-test-jwt-key-2026"));
    private static readonly RegistrarMensajeOutboxRequest Request =
        new("ventas.factura.emitida", "{\"factura\":\"masked\"}", "Factura", "123", "opaque-correlation");

    private sealed class ObservedService : IMensajeOutboxService
    {
        public int Calls { get; private set; }
        public string? Key { get; private set; }
        public Guid? EventId { get; private set; }
        public Exception? Failure { get; init; }
        public Task<MensajeOutbox> RegistrarAsync(int empresaId, RegistrarMensajeOutboxRequest request,
            string claveIdempotencia, CancellationToken cancellationToken = default)
        {
            Calls++; Key = claveIdempotencia;
            if (Failure is not null) return Task.FromException<MensajeOutbox>(Failure);
            var entity = MensajeOutbox.Crear(empresaId, request.TipoEvento, request.PayloadJson,
                claveIdempotencia, request.TipoAgregado, request.IdAgregado, request.CorrelationId);
            EventId = entity.EventoId;
            return Task.FromResult(entity);
        }
    }
    private sealed class Fixture : IDisposable
    {
        public required IHost Host { get; init; }
        public required Mock<IPermisoService> Grants { get; init; }
        public required ObservedService Service { get; init; }
        public HttpClient Client => Host.GetTestClient();
        public void Dispose() => Host.Dispose();
    }
    private static async Task<Fixture> StartAsync(bool revoked = false, Exception? failure = null)
    {
        var grants = new Mock<IPermisoService>(MockBehavior.Strict);
        grants.Setup(x => x.VerificarPermisoAsync(7, ModuloSistema.Configuracion,
            AccionPermiso.Crear)).Returns(revoked
                ? Task.FromException(new ForbiddenAccessException("provider-secret-grant"))
                : Task.CompletedTask);
        var service = new ObservedService { Failure = failure };
        var host = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(s =>
            {
                s.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase("c1103-m0502-" + Guid.NewGuid().ToString("N")));
                s.AddSingleton<IPermisoService>(grants.Object);
                s.AddSingleton<IMensajeOutboxService>(service);
                s.AddRouting();
                s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true, ValidIssuer = Issuer,
                        ValidateAudience = true, ValidAudience = Audience,
                        ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                        ValidateIssuerSigningKey = true, IssuerSigningKey = Key,
                        RequireSignedTokens = true, RequireExpirationTime = true
                    });
                s.AddAuthorization();
                s.AddControllers().AddApplicationPart(typeof(MensajesOutboxController).Assembly);
            })
            .Configure(app =>
            {
                // Only test middleware translates authorization filter exceptions.
                app.Use(async (ctx, next) =>
                {
                    try { await next(); }
                    catch (ForbiddenAccessException)
                    {
                        ctx.Response.Clear();
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await ctx.Response.WriteAsync("OUTBOX_PERMISSION_DENIED");
                    }
                });
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).StartAsync();
        return new Fixture { Host = host, Grants = grants, Service = service };
    }
    private static string Token(bool genuine = true)
    {
        var jwt = new JwtSecurityToken(issuer: Issuer, audience: Audience,
            claims: [new Claim("id", "17"), new Claim("nombreUsuario", "user.m0502")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                genuine ? Key : Forged, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
    private static HttpRequestMessage Post(int routeTenant = 7,
        string? bearer = null, string? key = "m0502-idempotency-key-123")
    {
        var req = new HttpRequestMessage(HttpMethod.Post,
            "/api/outbox/tenants/" + routeTenant) { Content = JsonContent.Create(Request) };
        req.Headers.Add("X-Empresa-Id", "7");
        if (bearer is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (key is not null) req.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        return req;
    }
    private static async Task AssertSafeProblem(HttpResponseMessage response,
        HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(code, body, StringComparison.Ordinal);
        Assert.DoesNotContain("provider-secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mysql://", body, StringComparison.OrdinalIgnoreCase);
    }
    [Fact] public async Task AnonymousDeniedBeforeController()
    {
        using var f = await StartAsync(); using var client = f.Client;
        using var req = Post(); using var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal(0, f.Service.Calls);
        f.Grants.VerifyNoOtherCalls();
    }
    [Fact] public async Task ForgedJwtDeniedBeforeController()
    {
        using var f = await StartAsync(); using var client = f.Client;
        using var req = Post(bearer: Token(false)); using var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal(0, f.Service.Calls);
        f.Grants.VerifyNoOtherCalls();
    }
    [Fact] public async Task ValidJwtReturnsDto202WithNormalizedIdempotency()
    {
        using var f = await StartAsync(); using var client = f.Client;
        using var req = Post(bearer: Token(), key: " m0502-idempotency-key-123 ");
        using var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal(1, f.Service.Calls);
        Assert.Equal("m0502-idempotency-key-123", f.Service.Key);
        Assert.NotNull(f.Service.EventId);
        Assert.Contains(f.Service.EventId.Value.ToString("D"), body,
            StringComparison.OrdinalIgnoreCase);
        f.Grants.Verify(x => x.VerificarPermisoAsync(
            7, ModuloSistema.Configuracion, AccionPermiso.Crear), Times.Once);
    }
    [Fact] public async Task TenantBRouteCannotBorrowTenantAGrant()
    {
        using var f = await StartAsync(); using var client = f.Client;
        using var req = Post(routeTenant: 8, bearer: Token());
        using var res = await client.SendAsync(req);
        await AssertSafeProblem(res, HttpStatusCode.Forbidden, "OUTBOX_TENANT_CONTEXT_MISMATCH");
        Assert.Equal(0, f.Service.Calls);
    }
    [Fact] public async Task RevokedPermissionCannotReachService()
    {
        using var f = await StartAsync(revoked: true); using var client = f.Client;
        using var req = Post(bearer: Token()); using var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(0, f.Service.Calls);
        f.Grants.Verify(x => x.VerificarPermisoAsync(
            7, ModuloSistema.Configuracion, AccionPermiso.Crear), Times.Once);
    }
    [Fact] public async Task MissingIdempotencyIsSafe400()
    {
        using var f = await StartAsync(); using var client = f.Client;
        using var req = Post(bearer: Token(), key: null);
        using var res = await client.SendAsync(req);
        await AssertSafeProblem(res, HttpStatusCode.BadRequest, "OUTBOX_IDEMPOTENCY_KEY_REQUIRED");
        Assert.Equal(0, f.Service.Calls);
    }
    [Fact] public async Task DuplicateConflictIsSafe409()
    {
        using var f = await StartAsync(failure:
            new ConflictException("provider-secret-conflict"));
        using var client = f.Client; using var req = Post(bearer: Token());
        using var res = await client.SendAsync(req);
        await AssertSafeProblem(res, HttpStatusCode.Conflict, "OUTBOX_IDEMPOTENCY_CONFLICT");
    }
    [Fact] public async Task InvalidDtoIsSafe400()
    {
        using var f = await StartAsync(failure: new ArgumentException("provider-secret-invalid"));
        using var client = f.Client; using var req = Post(bearer: Token());
        using var res = await client.SendAsync(req);
        await AssertSafeProblem(res, HttpStatusCode.BadRequest, "OUTBOX_INVALID_REQUEST");
    }
    [Fact] public async Task UniqueIndexRaceIsSafe409()
    {
        using var f = await StartAsync(failure: new DbUpdateException(
            "UX_MensajesOutbox_EmpresaId_ClaveIdempotencia provider-secret",
            new InvalidOperationException("mysql://provider-secret")));
        using var client = f.Client; using var req = Post(bearer: Token());
        using var res = await client.SendAsync(req);
        await AssertSafeProblem(res, HttpStatusCode.Conflict, "OUTBOX_IDEMPOTENCY_RACE");
    }
}
