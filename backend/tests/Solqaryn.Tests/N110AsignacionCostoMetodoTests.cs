using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N110AsignacionCostoMetodoTests
{
    [Fact]
    public void Solo_fifo_puede_referenciar_capa_contable()
    {
        Assert.Throws<ArgumentException>(() => AsignacionCostoMovimientoInventario.Crear(
            202, 10, MetodoCosteoInventario.PromedioPonderado, 1, 45m, "n110-salida-202", 101));
        Assert.Throws<ArgumentException>(() => AsignacionCostoMovimientoInventario.Crear(
            203, 10, MetodoCosteoInventario.Estandar, 1, 45m, "n110-salida-203", 101));

        var fifo = AsignacionCostoMovimientoInventario.Crear(
            204, 10, MetodoCosteoInventario.FIFO, 1, 45m, "n110-salida-204", 101);

        Assert.Equal(101, fifo.CapaCostoInventarioId);
    }
}
