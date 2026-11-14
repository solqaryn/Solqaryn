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
/// P2-016.D: el dato sintetico historico sin EmpresaId no debe recibir
/// un propietario por SaveChanges posterior bajo A/B, ni al reabrir otro
/// DbContext. EF InMemory: no acredita autorizacion HTTP ni migracion SQL.
/// </summary>
public sealed class P2016DDatosHistoricosPersistenciaTests
{
    [Fact]
    public async Task Guardados_por_tenant_no_reasignan_historico_sin_owner_en_nuevo_contexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2016d-" + Guid.NewGuid().ToString("N"))
            .Options;
        var seedHttp = new DefaultHttpContext();
        int historicaId;
        int propiaAId;
        int propiaBId;
        int empresaAId;
        int empresaBId;

        await using (var seed = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = seedHttp }))
        {
            var empresaA = new Empresa("P2016D Empresa A");
            var empresaB = new Empresa("P2016D Empresa B");
            seed.Set<Empresa>().AddRange(empresaA, empresaB);
            await seed.SaveChangesAsync();
            empresaAId = empresaA.Id;
            empresaBId = empresaB.Id;

            // Dato previo a la atribucion RBAC: nunca inferir propietario.
            var historica = new Categoria { Nombre = "P2016D Sin propietario" };
            seed.Categorias.Add(historica);
            await seed.SaveChangesAsync();
            historicaId = historica.Id;
            Assert.Null(seed.Entry(historica).Property<int?>("EmpresaId").CurrentValue);

            seedHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaAId;
            var propiaA = new Categoria { Nombre = "P2016D Propia A" };
            seed.Categorias.Add(propiaA);
            await seed.SaveChangesAsync();
            propiaAId = propiaA.Id;

            seedHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaBId;
            var propiaB = new Categoria { Nombre = "P2016D Propia B" };
            seed.Categorias.Add(propiaB);
            await seed.SaveChangesAsync();
            propiaBId = propiaB.Id;
        }

        // El contexto nuevo comprueba datos persistidos, no solo el change tracker original.
        var readHttp = new DefaultHttpContext();
        await using var read = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = readHttp });

        readHttp.Request.Headers["X-Empresa-Id"] = empresaBId.ToString();
        readHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaAId;
        Assert.Equal(new[] { propiaAId },
            await read.Categorias.AsNoTracking().Select(c => c.Id).ToArrayAsync());

        readHttp.Request.Headers["X-Empresa-Id"] = empresaAId.ToString();
        readHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaBId;
        Assert.Equal(new[] { propiaBId },
            await read.Categorias.AsNoTracking().Select(c => c.Id).ToArrayAsync());

        // IgnoreQueryFilters es solo una verificacion interna de procedencia.
        // No representa un endpoint publico ni una autorizacion del usuario.
        var ownerHistorico = await read.Categorias.IgnoreQueryFilters()
            .AsNoTracking().Where(c => c.Id == historicaId)
            .Select(c => EF.Property<int?>(c, "EmpresaId")).SingleAsync();
        Assert.Null(ownerHistorico);
        Assert.Equal(3, await read.Categorias.IgnoreQueryFilters().CountAsync());
    }
}
