using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-007.A: regresion combinada de ajuste y Kardex por EmpresaId verificado.
/// Complementa P2-007.C/D/E, sin reemplazar cobertura de transferencias,
/// conteos, reservas, valorizacion, HTTP/JWT o MySQL real.
/// </summary>
public sealed class P2007AInventarioKardexCierreTenantTests
{
    [Fact]
    public async Task Ajustes_y_movimientos_no_comparten_lecturas_ni_totales_con_tenant_ajeno()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2007a-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2007A Inventario A");
        var empresaB = new Empresa("P2007A Inventario B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = "P2007A producto A" };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();
        var ajusteA = new AjusteInventario
        {
            NumeroAjuste = "P2007A-COMPARTIDO", Motivo = "Conteo A"
        };
        var movimientoA = NuevoMovimiento(productoA.Id, 3);
        db.Set<AjusteInventario>().Add(ajusteA);
        db.MovimientosInventario.Add(movimientoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = "P2007A producto B" };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        var ajusteB = new AjusteInventario
        {
            NumeroAjuste = "P2007A-COMPARTIDO", Motivo = "Conteo B"
        };
        var movimientoB = NuevoMovimiento(productoB.Id, 7);
        db.Set<AjusteInventario>().Add(ajusteB);
        db.MovimientosInventario.Add(movimientoB);
        await db.SaveChangesAsync();

        Assert.Equal(empresaA.Id, db.Entry(ajusteA).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(empresaB.Id, db.Entry(ajusteB).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(empresaA.Id, db.Entry(movimientoA).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(empresaB.Id, db.Entry(movimientoB).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(2, await db.Set<AjusteInventario>().IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.MovimientosInventario.IgnoreQueryFilters().CountAsync());

        // El header que afirma B nunca sustituye la marca privada verificada A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(new[] { ajusteA.Id }, await AjustesVisiblesAsync(db));
        Assert.Equal(new[] { movimientoA.Id }, await MovimientosVisiblesAsync(db));
        Assert.Equal(3, await db.MovimientosInventario.AsNoTracking().SumAsync(m => m.Cantidad));
        Assert.Null(await db.MovimientosInventario.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == movimientoB.Id));

        // La misma instancia de DbContext debe reevaluar el tenant al pasar a B.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(new[] { ajusteB.Id }, await AjustesVisiblesAsync(db));
        Assert.Equal(new[] { movimientoB.Id }, await MovimientosVisiblesAsync(db));
        Assert.Equal(7, await db.MovimientosInventario.AsNoTracking().SumAsync(m => m.Cantidad));
        Assert.Null(await db.Set<AjusteInventario>().AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == ajusteA.Id));

        // La marca pública solo es válida bajo /tienda y no habilita Kardex privado.
        http.Request.Path = "/tienda";
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(empresaA.Id, db.AuthorizedReadEmpresaId);
        Assert.True(db.IsPublicStoreOnlyReadRequest);
        Assert.Empty(await AjustesVisiblesAsync(db));
        Assert.Empty(await MovimientosVisiblesAsync(db));
    }

    private static async Task<int[]> AjustesVisiblesAsync(AppDbContext db) =>
        await db.Set<AjusteInventario>().AsNoTracking()
            .Where(a => a.NumeroAjuste == "P2007A-COMPARTIDO")
            .OrderBy(a => a.Id).Select(a => a.Id).ToArrayAsync();

    private static async Task<int[]> MovimientosVisiblesAsync(AppDbContext db) =>
        await db.MovimientosInventario.AsNoTracking()
            .OrderBy(m => m.Id).Select(m => m.Id).ToArrayAsync();

    private static MovimientoInventario NuevoMovimiento(int productoId, int cantidad) => new()
    {
        ProductoId = productoId,
        Tipo = TipoMovimientoInventario.Entrada,
        Cantidad = cantidad,
        StockAnterior = 0,
        StockNuevo = cantidad,
        CorrelationId = "p2007a:folio-compartido",
        ReferenciaTipo = "AjusteInventario",
        ReferenciaId = productoId
    };
}
