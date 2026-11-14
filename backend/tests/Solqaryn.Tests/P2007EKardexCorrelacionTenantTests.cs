using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-007.E: una colision de CorrelationId de Kardex entre empresas no puede
/// filtrar movimientos ajenos, aun con un header X-Empresa-Id falsificado.
/// Prueba EF InMemory: no certifica HTTP/JWT, MySQL ni autorizacion productiva.
/// </summary>
public sealed class P2007EKardexCorrelacionTenantTests
{
    [Fact]
    public async Task Correlacion_compartida_y_header_falso_solo_exponen_tenant_verificado()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2007e-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);

        var empresaA = new Empresa("P2007E Kardex A");
        var empresaB = new Empresa("P2007E Kardex B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string correlationId = "p2007e:coincidente";

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = "P2007E producto A" };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();
        var movimientoA = NuevoMovimiento(productoA.Id, correlationId);
        db.MovimientosInventario.Add(movimientoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = "P2007E producto B" };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        var movimientoB = NuevoMovimiento(productoB.Id, correlationId);
        db.MovimientosInventario.Add(movimientoB);
        await db.SaveChangesAsync();

        Assert.Equal(empresaA.Id, db.Entry(movimientoA).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(empresaB.Id, db.Entry(movimientoB).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(2, await db.MovimientosInventario.IgnoreQueryFilters()
            .CountAsync(m => m.CorrelationId == correlationId));
        db.ChangeTracker.Clear();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(11, empresaA.Id, 1, "Admin A", true));
        scope.Setup(s => s.ObtenerActualAsync(empresaB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(22, empresaB.Id, 1, "Admin B", true));
        var repo = new MovimientoInventarioRepository(db, scope.Object);

        // A sigue verificado aunque la cabecera no confiable señale B.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await repo.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            CorrelationId = correlationId
        });
        Assert.Equal(1, visiblesA.TotalCount);
        Assert.Equal(movimientoA.Id, Assert.Single(visiblesA.Items).Id);
        Assert.Empty(await repo.GetByProductoAsync(productoB.Id));

        // Cambio de empresa en el MISMO DbContext: B ve solo su propio Kardex.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visiblesB = await repo.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            CorrelationId = correlationId
        });
        Assert.Equal(1, visiblesB.TotalCount);
        Assert.Equal(movimientoB.Id, Assert.Single(visiblesB.Items).Id);
        Assert.Empty(await repo.GetByProductoAsync(productoA.Id));

        // Una marca publica o header nunca sustituyen el alcance RBAC privado.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var sinMarca = await repo.GetPagedAsync(new MovimientoInventarioQueryDto
        {
            CorrelationId = correlationId
        });
        Assert.Equal(0, sinMarca.TotalCount);
        Assert.Empty(sinMarca.Items);
        scope.Verify(s => s.ObtenerActualAsync(), Times.Never);
    }

    private static MovimientoInventario NuevoMovimiento(int productoId, string correlationId) => new()
    {
        ProductoId = productoId,
        Tipo = TipoMovimientoInventario.Entrada,
        Cantidad = 1,
        StockAnterior = 0,
        StockNuevo = 1,
        CorrelationId = correlationId,
        ReferenciaTipo = "AjusteInventario",
        ReferenciaId = productoId
    };
}
