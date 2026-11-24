using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>C02-01.M05.02: actual signed JWT middleware and TestServer HTTP.
/// Mocked membership authority does NOT certify MySQL/runtime membership.</summary>
public sealed class C0201M0502TenantContextHttpApiContractTests
{
    private const string Issuer = "c0201-m0502-test-issuer";
    private const string Audience = "c0201-m0502-test-audience";
    private static readonly SymmetricSecurityKey Key =
        new(Encoding.UTF8.GetBytes("c0201-m0502-ephemeral-test-key-never-production-2026"));

    private static Mock<IUsuarioScopeService> Authority()
    {
        var authority = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        authority.Setup(x => x.SesionBaseVigenteAsync(17, 999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        authority.Setup(x => x.ListarEmpresasSeleccionablesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmpresaSeleccionable[] { new(7, "Tenant A") });
        authority.Setup(x => x.ObtenerActualAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(17, 7, 11, "Consulta A", false));
        authority.Setup(x => x.ObtenerActualAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        return authority;
    }

    [Fact]
    public async Task AnonymousAndForgedJwtCannotCallTenantEndpoints()
    {
        var authority = Authority();
        using var host = await HostFor(authority.Object);
        using var client = host.GetTestClient();
        using (var anon = await client.GetAsync("/tenant-context/empresas"))
            Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);
        using (var anonContext = await client.GetAsync("/tenant-context/7"))
            Assert.Equal(HttpStatusCode.Unauthorized, anonContext.StatusCode);
        var pieces = SignedToken().Split('.');
        pieces[2] = (pieces[2][0] == 'A' ? "B" : "A") + pieces[2][1..];
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", string.Join(".", pieces));
        using (var forged = await client.GetAsync("/tenant-context/7"))
            Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        authority.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task InvalidTenantIdHasSafe400WithoutConsultingMembership(int companyId)
    {
        var authority = Authority();
        using var host = await HostFor(authority.Object);
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", SignedToken());
        using var response = await client.GetAsync($"/tenant-context/{companyId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        if (json.RootElement.TryGetProperty("data", out var data))
            Assert.Equal(JsonValueKind.Null, data.ValueKind);
        authority.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SignedJwtApiAndDtoUseServerScopeNotSpoofedHostHeaderOrJwtRole()
    {
        var authority = Authority();
        using var host = await HostFor(authority.Object);
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", SignedToken());
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Empresa-Id", "8");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-Host", "tenant-b.invalid");
        using (var list = await client.GetAsync("/tenant-context/empresas"))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            var data = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
            Assert.Single(data);
            Assert.Equal(7, data[0].GetProperty("empresaId").GetInt32());
        }
        using (var response = await client.GetAsync("/tenant-context/7"))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = json.RootElement.GetProperty("data");
            Assert.Equal(17, data.GetProperty("usuarioId").GetInt32());
            Assert.Equal(7, data.GetProperty("empresaId").GetInt32());
            Assert.Equal(11, data.GetProperty("rolId").GetInt32());
            Assert.False(data.GetProperty("esAdministrador").GetBoolean());
        }
        // Positive selection must revalidate the destination; denial still reads once.
        authority.Verify(x => x.ObtenerActualAsync(7, It.IsAny<CancellationToken>()), Times.Exactly(2));
        authority.Verify(x => x.ListarEmpresasSeleccionablesAsync(It.IsAny<CancellationToken>()), Times.Once);
        authority.Verify(x => x.SesionBaseVigenteAsync(17, 999, It.IsAny<CancellationToken>()), Times.Exactly(4));
        authority.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NonMemberBIsOpaque403EvenWhenHeaderPointsToLegitimateA()
    {
        var authority = Authority();
        using var host = await HostFor(authority.Object);
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", SignedToken());
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Empresa-Id", "7");
        using var response = await client.GetAsync("/tenant-context/8");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Tenant A", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Consulta A", body, StringComparison.OrdinalIgnoreCase);
        authority.Verify(x => x.ObtenerActualAsync(8, It.IsAny<CancellationToken>()), Times.Once);
        authority.Verify(x => x.SesionBaseVigenteAsync(17, 999, It.IsAny<CancellationToken>()), Times.Once);
        authority.VerifyNoOtherCalls();
    }

    private static string SignedToken()
    {
        var jwt = new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: [new Claim("id", "17"), new Claim("rolId", "999"),
                new Claim("esAdministrador", "true")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static Task<IHost> HostFor(IUsuarioScopeService authority) =>
        new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IUsuarioScopeService>(authority);
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options => options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true, ValidateAudience = true,
                            ValidateLifetime = true, ValidateIssuerSigningKey = true,
                            ValidIssuer = Issuer, ValidAudience = Audience,
                            IssuerSigningKey = Key, ClockSkew = TimeSpan.Zero
                        });
                services.AddAuthorization();
                services.AddControllers().AddApplicationPart(typeof(TenantContextController).Assembly);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).StartAsync();
}
