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
/// P2-007.F: caso reproducible de una referencia de Kardex compartida por dos empresas.
/// La identidad de correlacion no concede visibilidad ni sustituye el tenant verificado.
/// Esta regresion EF InMemory no acredita HTTP/JWT ni el comportamiento MySQL real.
/// </summary>
public sealed class P2007FKardexCorrelationTenantTests
{
    [Fact]
    public async Task P2007F_CorrelationIdYReferenciaCoincidentes_SeAislanPorEmpresaVerificada()
    {
        const string correlacionCompartida = "P2007F-CORRELACION-COMPARTIDA";
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2007f-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2007F Kardex A");
        var empresaB = new Empresa("P2007F Kardex B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = "P2007F producto A" };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();
        var movimientoA = NuevoMovimiento(productoA.Id, 5, correlacionCompartida);
        db.MovimientosInventario.Add(movimientoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = "P2007F producto B" };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        var movimientoB = NuevoMovimiento(productoB.Id, 9, correlacionCompartida);
        db.MovimientosInventario.Add(movimientoB);
        await db.SaveChangesAsync();

        // El mismo CorrelationId + ReferenciaId puede coexistir en empresas diferentes.
        Assert.Equal(2, await db.MovimientosInventario.IgnoreQueryFilters()
            .CountAsync(m => m.CorrelationId == correlacionCompartida));
        Assert.Equal(empresaA.Id, db.Entry(movimientoA).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(empresaB.Id, db.Entry(movimientoB).Property<int?>("EmpresaId").CurrentValue);

        // Un header falsificado de B no puede cambiar el Kardex verificado de A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await VisiblesAsync(db, correlacionCompartida);
        Assert.Equal(new[] { movimientoA.Id }, visiblesA);
        Assert.Equal(5, await db.MovimientosInventario.AsNoTracking()
            .Where(m => m.CorrelationId == correlacionCompartida)
            .SumAsync(m => m.StockNuevo));

        // En el mismo DbContext, el cambio autenticado a B no arrastra el movimiento A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(new[] { movimientoB.Id }, await VisiblesAsync(db, correlacionCompartida));
        Assert.Equal(9, await db.MovimientosInventario.AsNoTracking()
            .Where(m => m.CorrelationId == correlacionCompartida)
            .SumAsync(m => m.StockNuevo));

        // Header A y marca PUBLICA B no autorizan lectura de Kardex comercial.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Empty(await VisiblesAsync(db, correlacionCompartida));
    }

    private static async Task<int[]> VisiblesAsync(AppDbContext db, string correlacion) =>
        await db.MovimientosInventario.AsNoTracking()
            .Where(m => m.CorrelationId == correlacion)
            .OrderBy(m => m.Id)
            .Select(m => m.Id)
            .ToArrayAsync();

    private static MovimientoInventario NuevoMovimiento(int productoId, int stockNuevo, string correlacion) => new()
    {
        ProductoId = productoId,
        Tipo = TipoMovimientoInventario.Entrada,
        Cantidad = stockNuevo,
        StockAnterior = 0,
        StockNuevo = stockNuevo,
        CorrelationId = correlacion,
        ReferenciaTipo = "AjusteInventario",
        ReferenciaId = 42
    };
}
