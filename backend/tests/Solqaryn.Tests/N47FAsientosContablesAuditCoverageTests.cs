using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N47FAsientosContablesAuditCoverageTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly AsientosContablesController _controller;

    public N47FAsientosContablesAuditCoverageTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _auditoriaMock = new Mock<IAuditoriaService>();
        _auditoriaMock.Setup(a => a.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        _controller = new AsientosContablesController(_db, _auditoriaMock.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task Crear_asiento_registra_auditoria_estricta_con_datos_correctos()
    {
        _db.Set<CuentaContable>().Add(new CuentaContable { Id = 10, Codigo = "1-1-1", Nombre = "Caja", Activa = true, AceptaMovimientos = true });
        _db.Set<CuentaContable>().Add(new CuentaContable { Id = 20, Codigo = "1-1-2", Nombre = "Banco", Activa = true, AceptaMovimientos = true });
        await _db.SaveChangesAsync();
        var dto = new CrearAsientoContableDto
        {
            Concepto = "Apertura de caja",
            Detalles =
            {
                new CrearAsientoDetalleDto { CuentaContableId = 10, Debe = 500m },
                new CrearAsientoDetalleDto { CuentaContableId = 20, Haber = 500m }
            }
        };
        var actionResult = await _controller.Create(dto, default);
        Assert.IsType<CreatedAtActionResult>(actionResult);
        _auditoriaMock.Verify(a => a.RegistrarEstrictoAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Crear,
            It.Is<string>(s => s.Contains("Registró el asiento contable")),
            It.Is<int?>(id => id > 0),
            "AsientoContable",
            null,
            It.Is<object?>(o => o != null &&
                o.GetType().GetProperty("Concepto") != null &&
                (string?)o.GetType().GetProperty("Concepto")!.GetValue(o) == "Apertura de caja" &&
                o.GetType().GetProperty("TotalDebe") != null &&
                (decimal?)o.GetType().GetProperty("TotalDebe")!.GetValue(o) == 500m &&
                o.GetType().GetProperty("TotalHaber") != null &&
                (decimal?)o.GetType().GetProperty("TotalHaber")!.GetValue(o) == 500m),
            null,
            "Exito",
            null), Times.Once);
    }

    [Fact]
    public async Task Crear_asiento_con_cuenta_inactiva_falla_y_no_registra_auditoria()
    {
        _db.Set<CuentaContable>().Add(new CuentaContable { Id = 10, Codigo = "1-1-1", Nombre = "Caja", Activa = false, AceptaMovimientos = true });
        _db.Set<CuentaContable>().Add(new CuentaContable { Id = 20, Codigo = "1-1-2", Nombre = "Banco", Activa = true, AceptaMovimientos = true });
        await _db.SaveChangesAsync();
        var dto = new CrearAsientoContableDto
        {
            Concepto = "Apertura de caja",
            Detalles =
            {
                new CrearAsientoDetalleDto { CuentaContableId = 10, Debe = 500m },
                new CrearAsientoDetalleDto { CuentaContableId = 20, Haber = 500m }
            }
        };
        await Assert.ThrowsAsync<BusinessRuleException>(() => _controller.Create(dto, default));
        _auditoriaMock.Verify(a => a.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
