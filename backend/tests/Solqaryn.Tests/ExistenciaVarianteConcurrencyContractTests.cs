using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class ExistenciaVarianteConcurrencyContractTests
{
    [Fact]
    public void Demanda_ExponeClaveFisicaCompleta()
    {
        var demanda = new InventarioDemandaExistencia(
            ProductoId: 10,
            ProductoVarianteId: 20,
            AlmacenId: 30,
            UbicacionAlmacenId: 40,
            Cantidad: 5);

        Assert.Equal(
            new InventarioExistenciaClave(20, 30, 40),
            demanda.Clave);
    }

    [Fact]
    public void Clave_PreservaUbicacionRaizComoNula()
    {
        var demanda = new InventarioDemandaExistencia(10, 20, 30, null, 5);

        Assert.Null(demanda.Clave.UbicacionAlmacenId);
    }

    [Fact]
    public async Task Bloqueo_ExigeTransaccionActiva_AntesDeConsultarRepositorio()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n14d-lock-{Guid.NewGuid():N}")
            .Options;
        await using var context = new AppDbContext(options);
        var repository = new Mock<IExistenciaVarianteRepository>(MockBehavior.Strict);
        var service = new ExistenciaVarianteConcurrencyService(context, repository.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.BloquearYValidarExistenciasAsync(new[]
            {
                new InventarioDemandaExistencia(10, 20, 30, null, 1)
            }));

        Assert.Contains("transacción activa", ex.Message, StringComparison.OrdinalIgnoreCase);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Ajuste_ExigeTransaccionActiva_AntesDeConsultarRepositorio()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n14d-adjust-{Guid.NewGuid():N}")
            .Options;
        await using var context = new AppDbContext(options);
        var repository = new Mock<IExistenciaVarianteRepository>(MockBehavior.Strict);
        var service = new ExistenciaVarianteConcurrencyService(context, repository.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AjustarStockFisicoPesimistaAsync(
                new InventarioExistenciaClave(20, 30, null),
                cantidadActualEsperada: 5,
                cantidadNueva: 6));

        Assert.Contains("transacción activa", ex.Message, StringComparison.OrdinalIgnoreCase);
        repository.VerifyNoOtherCalls();
    }
}
