using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-003.G: una cabecera o un query EmpresaId no concede acceso HTTP a Clientes
/// en la cuarentena multiempresa. Se prueba el middleware real con datos A/B
/// sinteticos; no reemplaza JWT/RBAC MySQL ni la certificacion E2E del proveedor.
/// </summary>
public sealed class P2003GClientesHttpTenantNegativoTests
{
    [Theory]
    [InlineData("/clientes")]
    [InlineData("/clientes/activos")]
    [InlineData("/clientes/buscar")]
    [InlineData("/clientes/2002")]
    public async Task MarcaNoVerificadaYCabeceraEmpresaFalsa_NoRevelanClientes(string ruta)
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2003g-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2003G Empresa A sintética");
        var empresaB = new Empresa("P2003G Empresa B sintética");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        db.Clientes.Add(new Cliente
        {
            EmpresaId = empresaA.Id,
            Nombre = "P2003G_CLIENTE_A_PRIVADO",
            IdentidadORTN = "P2003G-A-ONLY"
        });
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        db.Clientes.Add(new Cliente
        {
            EmpresaId = empresaB.Id,
            Nombre = "P2003G_CLIENTE_B_PRIVADO",
            IdentidadORTN = "P2003G-B-ONLY"
        });
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.Clientes.IgnoreQueryFilters().CountAsync());

        // Una cabecera forjada y un query string coincidentes no son marca
        // autorizada por el backend. Incluso una identidad autenticada simulada
        // no habilita automaticamente el ERP bajo cuarentena multiempresa.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(db.AuthorizedReadEmpresaId);
        http.Request.Method = "GET";
        http.Request.Path = ruta;
        http.Request.QueryString = new QueryString("?EmpresaId=" + empresaB.Id);
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "99") }, "Bearer"));
        http.Response.Body = new MemoryStream();

        var pasoAlControlador = false;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(context =>
        {
            pasoAlControlador = true;
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(http, db);

        Assert.False(pasoAlControlador);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, http.Response.StatusCode);
        http.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(http.Response.Body);
        Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED",
            payload.RootElement.GetProperty("code").GetString());

        var texto = payload.RootElement.ToString();
        Assert.DoesNotContain("P2003G_CLIENTE_A_PRIVADO", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("P2003G_CLIENTE_B_PRIVADO", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("P2003G-A-ONLY", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("P2003G-B-ONLY", texto, StringComparison.Ordinal);
    }
}
