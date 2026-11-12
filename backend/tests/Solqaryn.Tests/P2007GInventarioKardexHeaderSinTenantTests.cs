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
/// P2-007.G / Plan Comercial Inicial: negativo dirigido de Inventario/Kardex
/// cuando el atacante suplanta EmpresaId en cabecera y query, sin marca
/// de empresa privada verificada por el servidor. Usa EF InMemory; no acredita
/// middleware HTTP/JWT ni MySQL real.
/// </summary>
public sealed class P2007GInventarioKardexHeaderSinTenantTests
{
    [Fact]
    public async Task HeaderYQueryEmpresaId_NoConcedenKardexPrivadoSinMarcaVerificada()
    {
        const string correlacion = "P2007G-CORRELACION-COMPARTIDA";
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/inventario/kardex";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2007g-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2007G A");
        var empresaB = new Empresa("P2007G B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = "P2007G Producto A" };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();
        var movimientoA = NuevoMovimiento(productoA.Id, 3, correlacion);
        var ajusteA = new AjusteInventario { NumeroAjuste = "P2007G-FOLIO", Motivo = "A" };
        db.MovimientosInventario.Add(movimientoA);
        db.Set<AjusteInventario>().Add(ajusteA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = "P2007G Producto B" };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        var movimientoB = NuevoMovimiento(productoB.Id, 7, correlacion);
        var ajusteB = new AjusteInventario { NumeroAjuste = "P2007G-FOLIO", Motivo = "B" };
        db.MovimientosInventario.Add(movimientoB);
        db.Set<AjusteInventario>().Add(ajusteB);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.MovimientosInventario.IgnoreQueryFilters()
            .CountAsync(m => m.CorrelationId == correlacion));
        Assert.Equal(2, await db.Set<AjusteInventario>().IgnoreQueryFilters()
            .CountAsync(a => a.NumeroAjuste == "P2007G-FOLIO"));

        // Cabecera + query + marca publica no deben sustituir la marca privada.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Request.QueryString = new QueryString(
            $"?EmpresaId={empresaB.Id}&ReferenciaId=42&CorrelationId={correlacion}");
        Assert.Null(db.AuthorizedCommercialEmpresaId);
        Assert.False(db.IsPublicStoreOnlyReadRequest);
        Assert.Empty(await VisiblesKardex(db, correlacion));
        Assert.Empty(await VisiblesAjustes(db));

        // Control positivo: la marca interna de A prevalece sobre el header B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(new[] { movimientoA.Id }, await VisiblesKardex(db, correlacion));
        Assert.Equal(new[] { ajusteA.Id }, await VisiblesAjustes(db));
        Assert.Null(await db.MovimientosInventario.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == movimientoB.Id));

        // Cambio verificado a B en el mismo DbContext: A deja de ser visible.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        Assert.Equal(new[] { movimientoB.Id }, await VisiblesKardex(db, correlacion));
        Assert.Equal(new[] { ajusteB.Id }, await VisiblesAjustes(db));
        Assert.Null(await db.Set<AjusteInventario>().AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == ajusteA.Id));
    }

    private static Task<int[]> VisiblesKardex(AppDbContext db, string correlacion) =>
        db.MovimientosInventario.AsNoTracking()
            .Where(m => m.CorrelationId == correlacion)
            .OrderBy(m => m.Id).Select(m => m.Id).ToArrayAsync();

    private static Task<int[]> VisiblesAjustes(AppDbContext db) =>
        db.Set<AjusteInventario>().AsNoTracking()
            .Where(a => a.NumeroAjuste == "P2007G-FOLIO")
            .OrderBy(a => a.Id).Select(a => a.Id).ToArrayAsync();

    private static MovimientoInventario NuevoMovimiento(int productoId, int cantidad, string correlacion) => new()
    {
        ProductoId = productoId,
        Tipo = TipoMovimientoInventario.Entrada,
        Cantidad = cantidad,
        StockAnterior = 0,
        StockNuevo = cantidad,
        CorrelationId = correlacion,
        ReferenciaTipo = "AjusteInventario",
        ReferenciaId = 42
    };
}
