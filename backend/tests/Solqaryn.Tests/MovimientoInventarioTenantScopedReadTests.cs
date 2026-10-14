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

public sealed class MovimientoInventarioTenantScopedReadTests
{
    [Fact]
    public async Task Kardex_A_B_NuncaMuestraMovimientosAjenosNiAceptaSoloTiendaPublica()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-kardex-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);
        var empresaA = new Empresa("Kardex A");
        var empresaB = new Empresa("Kardex B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = "Privado A" };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();
        var movA = NuevoMovimiento(productoA.Id, "a");
        db.MovimientosInventario.Add(movA);
        await db.SaveChangesAsync();
        Assert.Equal(empresaA.Id, db.Entry(movA).Property<int?>("EmpresaId").CurrentValue);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = "Privado B" };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        var movB = NuevoMovimiento(productoB.Id, "b");
        db.MovimientosInventario.Add(movB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin global", true));
        scope.Setup(x => x.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(11, empresaA.Id, 1, "Admin A", true));
        scope.Setup(x => x.ObtenerActualAsync(empresaB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(22, empresaB.Id, 1, "Admin B", true));
        var repo = new MovimientoInventarioRepository(db, scope.Object);
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var a = await repo.GetPagedAsync(new MovimientoInventarioQueryDto());
        Assert.Equal(movA.Id, Assert.Single(a.Items).Id);
        Assert.Empty(await repo.GetByProductoAsync(productoB.Id));

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var other = await repo.GetFilteredAsync(null, null, null, null);
        Assert.Equal(movB.Id, Assert.Single(other).Id);

        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Path = "/tienda/productos";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var withoutRbac = await repo.GetPagedAsync(new MovimientoInventarioQueryDto());
        Assert.Equal(0, withoutRbac.TotalCount);
        scope.Verify(x => x.ObtenerActualAsync(), Times.Never);
    }

    private static MovimientoInventario NuevoMovimiento(int productoId, string suffix) => new()
    {
        ProductoId = productoId, Tipo = TipoMovimientoInventario.Entrada,
        Cantidad = 1, StockAnterior = 0, StockNuevo = 1,
        CorrelationId = "p4-test:" + suffix,
        ReferenciaTipo = "AjusteInventario", ReferenciaId = productoId
    };
}
