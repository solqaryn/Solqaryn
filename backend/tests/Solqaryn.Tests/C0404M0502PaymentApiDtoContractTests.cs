using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>C04-04.M05.02: real MVC, signed JWT and TestServer HTTP contract.
/// Membership and permissions are mocked, EF is InMemory; no real PSP or SQL integration.</summary>
public sealed class C0404M0502PaymentApiDtoContractTests
{
    private const string Issuer = "c0404-m0502-isolated-test-issuer";
    private const string Audience = "c0404-m0502-isolated-test-client";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0404-m0502-valid-ephemeral-test-signing-key-2026"));

    private sealed class Fixture : IDisposable
    {
        public required IHost Host { get; init; }
        public required Mock<IUsuarioScopeService> Scope { get; init; }
        public required Mock<IPermisoService> Grants { get; init; }
        public HttpClient Client => Host.GetTestClient();
        public void Dispose() => Host.Dispose();
    }

    private static async Task<Fixture> CreateAsync()
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(17, 7, 11, "Factura A", false));
        scope.Setup(x => x.ObtenerActualAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        var grants = new Mock<IPermisoService>(MockBehavior.Strict);
        grants.Setup(x => x.VerificarPermisoAsync(7, ModuloSistema.Facturacion,
            AccionPermiso.Aplicar)).Returns(Task.CompletedTask);
        grants.Setup(x => x.VerificarPermisoAsync(7, ModuloSistema.Facturacion,
            AccionPermiso.Ver)).Returns(Task.CompletedTask);
        var facturas = new Mock<IFacturaRepository>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var host = await new HostBuilder().ConfigureWebHost(web => web
            .UseTestServer()
            .ConfigureServices(s =>
            {
                s.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase("c0404-m0502-" + Guid.NewGuid().ToString("N")));
                s.AddSingleton(scope.Object);
                s.AddSingleton(grants.Object);
                s.AddSingleton(facturas.Object);
                s.AddSingleton(currentUser.Object);
                s.AddRouting();
                s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true, ValidIssuer = Issuer,
                        ValidateAudience = true, ValidAudience = Audience,
                        ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                        ValidateIssuerSigningKey = true, IssuerSigningKey = Key
                    });
                s.AddAuthorization();
                s.AddControllers().AddApplicationPart(typeof(PagosOnlineController).Assembly);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(routes => routes.MapControllers());
            })).StartAsync();
        return new Fixture { Host = host, Scope = scope, Grants = grants };
    }

    private static string Token(bool correctlySigned)
    {
        var key = correctlySigned ? Key : new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("c0404-m0502-invalid-ephemeral-signing-key-2026"));
        var jwt = new JwtSecurityToken(issuer: Issuer, audience: Audience,
            claims: [new Claim("id", "17"), new Claim("rolId", "999")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static HttpRequestMessage Post(int tenant = 7, string? bearer = null,
        string? idempotency = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/pagos-online/iniciar")
        {
            Content = JsonContent.Create(new IniciarPagoOnlineDto(
                tenant, 41, "provider-not-configured", 50m, "HNL"))
        };
        req.Headers.Add("X-Empresa-Id", "7");
        if (bearer is not null)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (idempotency is not null)
            req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotency);
        return req;
    }

    [Fact]
    public async Task AnonymousCannotReachPaymentControllerOrProvider()
    {
        using var f = await CreateAsync(); using var client = f.Client;
        using var req = Post(idempotency: "idempotency-safe-test-000001");
        using var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        f.Scope.VerifyNoOtherCalls(); f.Grants.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForgedJwtCannotReachPaymentControllerOrProvider()
    {
        using var f = await CreateAsync(); using var client = f.Client;
        using var req = Post(bearer: Token(false), idempotency: "idempotency-safe-test-000001");
        using var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        f.Scope.VerifyNoOtherCalls(); f.Grants.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TenantBRevokedMembershipDoesNotInvokePaymentProvider()
    {
        using var f = await CreateAsync(); using var client = f.Client;
        using var req = Post(tenant: 8, bearer: Token(true),
            idempotency: "idempotency-safe-test-000001");
        using var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        f.Scope.Verify(x => x.ObtenerActualAsync(8, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignedJwtWithoutIdempotencyReturnsSafeProblemDetails400()
    {
        using var f = await CreateAsync(); using var client = f.Client;
        using var req = Post(bearer: Token(true));
        using var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idempotency-Key", body);
        Assert.DoesNotContain("mysql://", body, StringComparison.OrdinalIgnoreCase);
        f.Scope.Verify(x => x.ObtenerActualAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(129)]
    public async Task ShortOrOversizeIdempotencyReturnsSafe400(int size)
    {
        using var f = await CreateAsync(); using var client = f.Client;
        using var req = Post(bearer: Token(true), idempotency: new string('k', size));
        using var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idempotency-Key", body);
        Assert.DoesNotContain("provider-secret", body, StringComparison.OrdinalIgnoreCase);
    }
}
