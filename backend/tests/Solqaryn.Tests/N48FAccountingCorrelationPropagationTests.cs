using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N48FAccountingCorrelationPropagationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IAuditoriaService> _auditoriaMock;
    private readonly AsientoContableWriter _writer;

    public N48FAccountingCorrelationPropagationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);
        _auditoriaMock = new Mock<IAuditoriaService>();
        _writer = new AsientoContableWriter(_db, _auditoriaMock.Object);
    }

    [Fact]
    public async Task Crear_asiento_registra_auditoria_estricta_propagando_correlacion_de_documento_origen()
    {
        _db.Set<CuentaContable>().Add(new CuentaContable { Id = 10, Codigo = "1-1-1", Nombre = "Caja", Activa = true, AceptaMovimientos = true });
        _db.Set<CuentaContable>().Add(new CuentaContable { Id = 20, Codigo = "1-1-2", Nombre = "Banco", Activa = true, AceptaMovimientos = true });
        await _db.SaveChangesAsync();

        var dto = new CrearAsientoContableDto
        {
            Concepto = "Apertura de caja",
            DocumentoOrigenId = 1234,
            TipoDocumentoOrigen = "FacturaVenta",
            Detalles =
            {
                new CrearAsientoDetalleDto { CuentaContableId = 10, Debe = 500m },
                new CrearAsientoDetalleDto { CuentaContableId = 20, Haber = 500m }
            }
        };

        object? capturedValues = null;
        _auditoriaMock.Setup(a => a.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<ModuloSistema, AccionPermiso, string, int?, string?, object?, object?, string?, string, string?>(
                (mod, act, desc, refId, ent, oldVal, newVal, mot, res, err) => { capturedValues = newVal; })
            .Returns(Task.CompletedTask);

        await _writer.CreateAsync(dto, default);

        Assert.NotNull(capturedValues);
        var json = System.Text.Json.JsonSerializer.Serialize(capturedValues);
        Assert.Contains("\"DocumentoOrigenId\":1234", json);
        Assert.Contains("\"TipoDocumentoOrigen\":\"FacturaVenta\"", json);
    }

    [Fact]
    public async Task Crear_asiento_repite_idempotencia_retorna_registro_existente_sin_re_auditoria()
    {
        _db.Set<CuentaContable>().Add(new CuentaContable { Id = 10, Codigo = "1-1-1", Nombre = "Caja", Activa = true, AceptaMovimientos = true });
        _db.Set<CuentaContable>().Add(new CuentaContable { Id = 20, Codigo = "1-1-2", Nombre = "Banco", Activa = true, AceptaMovimientos = true });
        await _db.SaveChangesAsync();

        var dto = new CrearAsientoContableDto
        {
            Concepto = "Apertura de caja",
            Numero = "AS-9999",
            Detalles =
            {
                new CrearAsientoDetalleDto { CuentaContableId = 10, Debe = 500m },
                new CrearAsientoDetalleDto { CuentaContableId = 20, Haber = 500m }
            }
        };

        var res1 = await _writer.CreateAsync(dto, default);
        Assert.True(res1.Created);
        _auditoriaMock.Verify(a => a.RegistrarEstrictoAsync(
            It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(), It.IsAny<string>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);

        _auditoriaMock.Invocations.Clear();
        var res2 = await _writer.CreateAsync(dto, default);
        Assert.False(res2.Created);
        Assert.Equal(res1.Id, res2.Id);
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
