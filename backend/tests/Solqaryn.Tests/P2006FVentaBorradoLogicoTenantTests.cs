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
/// P2-006.F: caso causal preparado para ventas con borrado logico.
/// El filtro EF debe conservar simultaneamente tenant verificado y
/// !Eliminado, incluso si el cliente falsifica X-Empresa-Id.
/// EF InMemory no certifica HTTP, JWT, RBAC ni MySQL real.
/// </summary>
public sealed class P2006FVentaBorradoLogicoTenantTests
{
    [Fact]
    public async Task Venta_eliminada_no_reaparece_al_cambiar_tenant_y_cabecera_falsificada()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2006f-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var a = new Empresa("P2006F Empresa A");
        var b = new Empresa("P2006F Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var activaA = new Venta
        {
            EmpresaId = a.Id,
            NumeroVenta = "P2006F-A-VIGENTE"
        };
        var eliminadaA = new Venta
        {
            EmpresaId = a.Id,
            NumeroVenta = "P2006F-A-ELIMINADA",
            Eliminado = true
        };
        db.Ventas.AddRange(activaA, eliminadaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        var activaB = new Venta
        {
            EmpresaId = b.Id,
            NumeroVenta = "P2006F-B-VIGENTE"
        };
        db.Ventas.Add(activaB);
        await db.SaveChangesAsync();

        // Solo para comprobar el fixture: tres registros persisten.
        // IgnoreQueryFilters NO es un mecanismo de autorizacion de runtime.
        Assert.Equal(3, await db.Ventas.IgnoreQueryFilters().CountAsync());

        // Marca backend A prevalece sobre el header adulterado B.
        http.Request.Headers["X-Empresa-Id"] = b.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var visiblesA = await db.Ventas.AsNoTracking()
            .OrderBy(v => v.Id)
            .Select(v => v.Id)
            .ToArrayAsync();
        Assert.Equal(new[] { activaA.Id }, visiblesA);
        Assert.DoesNotContain(eliminadaA.Id, visiblesA);
        Assert.DoesNotContain(activaB.Id, visiblesA);

        // Cambiar exclusivamente la marca verificada a B aisla sus ventas.
        http.Request.Headers["X-Empresa-Id"] = a.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        Assert.Equal(new[] { activaB.Id }, await db.Ventas.AsNoTracking()
            .Select(v => v.Id)
            .ToArrayAsync());

        // Volver a A conserva la exclusion logica y el aislamiento cruzado.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        Assert.Equal(new[] { activaA.Id }, await db.Ventas.AsNoTracking()
            .Select(v => v.Id)
            .ToArrayAsync());
    }
}
