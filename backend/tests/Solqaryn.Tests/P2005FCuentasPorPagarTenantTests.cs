using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-005.F: caso causal de cuentas por pagar en compras con ownership A/B.
/// EF InMemory: no certifica HTTP/JWT, transacciones ni MySQL real.
/// </summary>
public sealed class P2005FCuentasPorPagarTenantTests
{
    [Fact]
    public async Task Cuentas_por_pagar_aisla_tenant_y_excluye_pagadas_y_borradores()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2005f-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2005F Empresa A");
        var empresaB = new Empresa("P2005F Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        // Ambas empresas tienen un numero de compra homonimo. Solo las
        // confirmadas no pagadas deben entrar en el agregado de su tenant.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        db.Compras.AddRange(
            new Compra
            {
                EmpresaId = empresaA.Id,
                NumeroCompra = "P2005F-COMPARTIDA",
                ProveedorNombre = "Proveedor A",
                Estado = EstadoDocumento.Confirmada,
                EstadoPago = EstadoPago.Pendiente,
                Total = 125m
            },
            new Compra
            {
                EmpresaId = empresaA.Id,
                NumeroCompra = "P2005F-A-PAGADA",
                ProveedorNombre = "Proveedor A",
                Estado = EstadoDocumento.Confirmada,
                EstadoPago = EstadoPago.Pagado,
                Total = 600m
            },
            new Compra
            {
                EmpresaId = empresaA.Id,
                NumeroCompra = "P2005F-A-BORRADOR",
                ProveedorNombre = "Proveedor A",
                Estado = EstadoDocumento.Borrador,
                EstadoPago = EstadoPago.Pendiente,
                Total = 300m
            });
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaB.Id,
            NumeroCompra = "P2005F-COMPARTIDA",
            ProveedorNombre = "Proveedor B",
            Estado = EstadoDocumento.Confirmada,
            EstadoPago = EstadoPago.Pendiente,
            Total = 80m
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scopes = new Mock<IUsuarioScopeService>();
        scopes.Setup(s => s.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(91, empresaA.Id, 1, "Admin A", true));
        scopes.Setup(s => s.ObtenerActualAsync(empresaB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(91, empresaB.Id, 1, "Admin B", true));
        var compras = new CompraRepository(db, new Mock<ICurrentUserService>().Object, scopes.Object);

        // La cabecera del cliente contradice el tenant verificado del backend.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(125m, await compras.GetCuentasPorPagarAsync());

        // El mismo repositorio debe recalcular el alcance para B.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(80m, await compras.GetCuentasPorPagarAsync());

        // Con dos empresas, quitar la marca verificada deniega incluso
        // si el cliente sigue enviando la cabecera de un tenant real.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.Equal(0m, await compras.GetCuentasPorPagarAsync());
        scopes.Verify(s => s.ObtenerActualAsync(), Times.Never);
    }
}
