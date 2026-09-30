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

public sealed class N47FAsientosContablesAuditCorrelationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly AsientosContablesController _controller;

    public N47FAsientosContablesAuditCorrelationTests()
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
    public async Task Crear_asiento_registra_auditoria_estricta_propagando_fallos_de_auditoria_dentro_de_transaccion()
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

        _auditoriaMock.Setup(a => a.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>())).ThrowsAsync(new InvalidOperationException("audit-store-down"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(dto, default));
        Assert.Equal("audit-store-down", ex.Message);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
