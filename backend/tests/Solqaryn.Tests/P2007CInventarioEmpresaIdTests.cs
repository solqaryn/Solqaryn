using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class P2007CInventarioEmpresaIdTests
{
    [Fact]
    public async Task Ajustes_inventario_se_aislan_por_empresa_verificada()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2007c-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("Inventario A");
        var empresaB = new Empresa("Inventario B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ajusteA = new AjusteInventario { NumeroAjuste = "P2007C-A", Motivo = "Conteo A" };
        db.Set<AjusteInventario>().Add(ajusteA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ajusteB = new AjusteInventario { NumeroAjuste = "P2007C-B", Motivo = "Conteo B" };
        db.Set<AjusteInventario>().Add(ajusteB);
        await db.SaveChangesAsync();

        Assert.Equal(empresaA.Id, db.Entry(ajusteA).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(empresaB.Id, db.Entry(ajusteB).Property<int?>("EmpresaId").CurrentValue);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(new[] { ajusteA.Id },
            await db.Set<AjusteInventario>().AsNoTracking().Select(x => x.Id).ToArrayAsync());

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(new[] { ajusteB.Id },
            await db.Set<AjusteInventario>().AsNoTracking().Select(x => x.Id).ToArrayAsync());
    }
}
