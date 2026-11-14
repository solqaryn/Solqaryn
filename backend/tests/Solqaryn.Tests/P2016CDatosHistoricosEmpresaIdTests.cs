using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-016.C: datos sinteticos historicos sin propietario no se atribuyen
/// implicitamente a A o B por una cabecera X-Empresa-Id falsificada.
/// La ausencia de tenant en EF NO autoriza acceso HTTP multiempresa.
/// </summary>
public sealed class P2016CDatosHistoricosEmpresaIdTests
{
    [Fact]
    public async Task Registro_historico_sin_owner_no_se_atribuye_a_otro_tenant_por_cabecera()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2016c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2016C Empresa A");
        var empresaB = new Empresa("P2016C Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        // Dato historico de prueba, creado antes de recibir un tenant RBAC.
        var historica = new Categoria { Nombre = "P2016C Categoria legado" };
        db.Categorias.Add(historica);
        await db.SaveChangesAsync();
        Assert.Null(db.Entry(historica).Property<int?>("EmpresaId").CurrentValue);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var soloA = new Categoria { Nombre = "P2016C Categoria A" };
        db.Categorias.Add(soloA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var soloB = new Categoria { Nombre = "P2016C Categoria B" };
        db.Categorias.Add(soloB);
        await db.SaveChangesAsync();

        // Una cabecera controlada por el cliente no cambia la autoridad.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(empresaA.Id, db.AuthorizedReadEmpresaId);
        Assert.Equal(new[] { soloA.Id },
            await db.Categorias.AsNoTracking().Select(c => c.Id).ToArrayAsync());

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(new[] { soloB.Id },
            await db.Categorias.AsNoTracking().Select(c => c.Id).ToArrayAsync());

        // El registro legado permanece sin propietario; no se reasigna al leer.
        Assert.Null(db.Entry(historica).Property<int?>("EmpresaId").CurrentValue);
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(db.AuthorizedReadEmpresaId);
        Assert.Contains(await db.Categorias.AsNoTracking().ToArrayAsync(),
            c => c.Id == historica.Id);
        // La cuarentena HTTP P0 con 2+ empresas es un gate independiente,
        // ver UnscopedCommercialTenantGuardTests.DosEmpresas_NoPuedenRecibirDatosComerciales.
    }
}
