using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-007.D: folios identicos de ajustes de inventario no cruzan la lectura
/// entre empresas. EF InMemory no certifica endpoints HTTP, JWT ni MySQL.
/// </summary>
public sealed class P2007DInventarioFolioHomonimoTests
{
    [Fact]
    public async Task Numero_ajuste_homonimo_y_header_falso_no_cruzan_tenant_verificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2007d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2007D Inventario A");
        var empresaB = new Empresa("P2007D Inventario B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string numeroCompartido = "P2007D-AJUSTE-COMPARTIDO";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ajusteA = new AjusteInventario
        {
            NumeroAjuste = numeroCompartido,
            Motivo = "Conteo aislado A"
        };
        db.Set<AjusteInventario>().Add(ajusteA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ajusteB = new AjusteInventario
        {
            NumeroAjuste = numeroCompartido,
            Motivo = "Conteo aislado B"
        };
        db.Set<AjusteInventario>().Add(ajusteB);
        await db.SaveChangesAsync();

        Assert.NotEqual(ajusteA.Id, ajusteB.Id);
        Assert.Equal(empresaA.Id, db.Entry(ajusteA).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(empresaB.Id, db.Entry(ajusteB).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(2, await db.Set<AjusteInventario>().IgnoreQueryFilters()
            .CountAsync(a => a.NumeroAjuste == numeroCompartido));

        // La cabecera es de B, pero la autoridad verificada sigue siendo A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await db.Set<AjusteInventario>().AsNoTracking()
            .Where(a => a.NumeroAjuste == numeroCompartido)
            .Select(a => a.Id).ToArrayAsync();
        Assert.Equal(new[] { ajusteA.Id }, visiblesA);
        Assert.DoesNotContain(ajusteB.Id, visiblesA);

        // Reutilizar el mismo contexto evita ocultar un filtro tenant obsoleto.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visiblesB = await db.Set<AjusteInventario>().AsNoTracking()
            .Where(a => a.NumeroAjuste == numeroCompartido)
            .Select(a => a.Id).ToArrayAsync();
        Assert.Equal(new[] { ajusteB.Id }, visiblesB);
        Assert.DoesNotContain(ajusteA.Id, visiblesB);
    }
}
