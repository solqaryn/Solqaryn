using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class ContabilizacionServiceTests
{
    [Fact]
    public async Task ContabilizarAsync_ConfiguracionActiva_MapeaEventoYDelegaWriter()
    {
        await using var db = CreateDb();
        db.Set<ConfiguracionContable>().Add(new ConfiguracionContable
        {
            Evento = TipoEventoContable.Venta,
            CuentaDebeId = 101,
            CuentaHaberId = 202,
            Activo = true
        });
        await db.SaveChangesAsync();

        CrearAsientoContableDto? capturado = null;
        var writer = new Mock<IAsientoContableWriter>(MockBehavior.Strict);
        writer.Setup(x => x.CreateAsync(It.IsAny<CrearAsientoContableDto>(), It.IsAny<CancellationToken>()))
            .Callback<CrearAsientoContableDto, CancellationToken>((dto, _) => capturado = dto)
            .ReturnsAsync(new AsientoContableWriteResult(new AsientoContableDto { Id = 7 }, true, 7));

        var service = new ContabilizacionService(db, writer.Object);
        var fecha = new DateTime(2026, 9, 4, 18, 0, 0, DateTimeKind.Utc);

        var result = await service.ContabilizarAsync(new EventoContableDto(
            TipoEventoContable.Venta,
            DocumentoOrigenId: 55,
            Fecha: fecha,
            Monto: 1250m,
            Referencia: "  FAC-55  "));

        Assert.True(result.Created);
        Assert.NotNull(capturado);
        Assert.Equal("EVT-1-55", capturado!.Numero);
        Assert.Equal("Venta", capturado.TipoDocumentoOrigen);
        Assert.Equal(55, capturado.DocumentoOrigenId);
        Assert.Equal(fecha, capturado.Fecha);
        Assert.Equal("Venta: FAC-55", capturado.Concepto);
        Assert.Collection(capturado.Detalles,
            debe =>
            {
                Assert.Equal(101, debe.CuentaContableId);
                Assert.Equal(1250m, debe.Debe);
                Assert.Equal(0m, debe.Haber);
                Assert.Equal("FAC-55", debe.Referencia);
            },
            haber =>
            {
                Assert.Equal(202, haber.CuentaContableId);
                Assert.Equal(0m, haber.Debe);
                Assert.Equal(1250m, haber.Haber);
                Assert.Equal("FAC-55", haber.Referencia);
            });
        writer.VerifyAll();
    }

    [Fact]
    public async Task ContabilizarAsync_SinConfiguracion_FallaSinEscribir()
    {
        await using var db = CreateDb();
        var writer = new Mock<IAsientoContableWriter>(MockBehavior.Strict);
        var service = new ContabilizacionService(db, writer.Object);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.ContabilizarAsync(
            new EventoContableDto(
                TipoEventoContable.Pago,
                DocumentoOrigenId: 9,
                Fecha: DateTime.UtcNow,
                Monto: 10m,
                Referencia: "PAGO-9")));

        writer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ContabilizarAsync_ConfiguracionInactiva_FallaSinEscribir()
    {
        await using var db = CreateDb();
        db.Set<ConfiguracionContable>().Add(new ConfiguracionContable
        {
            Evento = TipoEventoContable.Cobro,
            CuentaDebeId = 301,
            CuentaHaberId = 401,
            Activo = false
        });
        await db.SaveChangesAsync();

        var writer = new Mock<IAsientoContableWriter>(MockBehavior.Strict);
        var service = new ContabilizacionService(db, writer.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ContabilizarAsync(
            new EventoContableDto(
                TipoEventoContable.Cobro,
                DocumentoOrigenId: 12,
                Fecha: DateTime.UtcNow,
                Monto: 99m,
                Referencia: "COBRO-12")));

        writer.VerifyNoOtherCalls();
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
