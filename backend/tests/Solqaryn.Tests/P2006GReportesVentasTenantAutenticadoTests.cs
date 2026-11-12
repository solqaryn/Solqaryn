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
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-006.G: regresion negativa del reporte de ventas por EmpresaId.
/// Una cabecera X-Empresa-Id controlada por el solicitante no otorga
/// autoridad de lectura. El servicio debe negar datos cuando falta la
/// marca comercial verificada o una membresia activa resuelta server-side.
/// EF InMemory no certifica el middleware HTTP/JWT/RBAC ni MySQL real.
/// </summary>
public sealed class P2006GReportesVentasTenantAutenticadoTests
{
    [Fact]
    public async Task Cabecera_suplantada_sin_marca_verificada_no_devuelve_ventas_de_ninguna_empresa()
    {
        var (db, http, empresaA, empresaB) = await CrearVentasABAsync();
        await using var ownedDb = db;

        // Control de fixture: existen dos ventas confirmadas y distintas.
        Assert.Equal(2, await db.Ventas.IgnoreQueryFilters().CountAsync());
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);

        // Con ausencia de marca backend, el servicio no debe consultar membresia
        // ni revelar ventas, aunque la cabecera alterne entre A y B.
        var scopes = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var servicio = new ReporteVentasService(db, scopes.Object);
        foreach (var empresaFalsificada in new[] { empresaA, empresaB })
        {
            http.Request.Headers["X-Empresa-Id"] = empresaFalsificada.ToString();
            var resumen = await servicio.ObtenerResumenAsync(new ReporteVentasFiltroDto());
            var detalle = await servicio.ObtenerDetallePaginadoAsync(new ReporteVentasFiltroDto());
            Assert.Equal(0m, resumen.Total);
            Assert.Equal(0, detalle.TotalCount);
            Assert.Empty(detalle.Items);
        }

        scopes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Marca_verificada_sin_membresia_activa_no_adopta_el_tenant_de_cabecera()
    {
        var (db, http, empresaA, empresaB) = await CrearVentasABAsync();
        await using var ownedDb = db;

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA;
        http.Request.Headers["X-Empresa-Id"] = empresaB.ToString();

        var scopes = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scopes.Setup(s => s.ObtenerActualAsync(empresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        var servicio = new ReporteVentasService(db, scopes.Object);

        var resumen = await servicio.ObtenerResumenAsync(new ReporteVentasFiltroDto());
        var detalle = await servicio.ObtenerDetallePaginadoAsync(new ReporteVentasFiltroDto());
        Assert.Equal(0m, resumen.Total);
        Assert.Equal(0, detalle.TotalCount);
        Assert.Empty(detalle.Items);
        scopes.Verify(s => s.ObtenerActualAsync(empresaA, It.IsAny<CancellationToken>()), Times.Exactly(2));
        scopes.VerifyNoOtherCalls();
    }

    private static async Task<(AppDbContext Db, DefaultHttpContext Http, int EmpresaA, int EmpresaB)>
        CrearVentasABAsync()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2006g-" + Guid.NewGuid().ToString("N"))
            .Options;
        var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var a = new Empresa("P2006G Empresa A");
        var b = new Empresa("P2006G Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        db.Ventas.Add(new Venta
        {
            EmpresaId = a.Id,
            NumeroVenta = "P2006G-A",
            Estado = EstadoDocumento.Confirmada,
            Total = 31m
        });
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        db.Ventas.Add(new Venta
        {
            EmpresaId = b.Id,
            NumeroVenta = "P2006G-B",
            Estado = EstadoDocumento.Confirmada,
            Total = 79m
        });
        await db.SaveChangesAsync();

        return (db, http, a.Id, b.Id);
    }
}
