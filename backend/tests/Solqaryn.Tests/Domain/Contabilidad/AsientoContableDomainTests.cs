using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests.Domain.Contabilidad;

public class AsientoContableDomainTests
{
    [Fact]
    public void Detalle_RechazaMontosInvalidos()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AsientoDetalle(1, -10m, 0m, "Test"));
        Assert.Throws<ArgumentException>(() => new AsientoDetalle(1, 0m, 0m, "Test"));
        Assert.Throws<ArgumentException>(() => new AsientoDetalle(1, 10m, 10m, "Test"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AsientoDetalle(0, 10m, 0m, "Test"));
    }

    [Fact]
    public void Asiento_RechazaVacioYDescuadrado()
    {
        var asiento = new AsientoContable();
        Assert.False(asiento.EstaCuadrado());
        Assert.Throws<InvalidOperationException>(() => asiento.ValidarCuadre());

        asiento.AgregarDetalle(new AsientoDetalle(1, 100m, 0m, "Debe"));
        asiento.AgregarDetalle(new AsientoDetalle(2, 0m, 50m, "Haber"));
        Assert.False(asiento.EstaCuadrado());
        Assert.Throws<InvalidOperationException>(() => asiento.ValidarCuadre());
    }

    [Fact]
    public void Asiento_AceptaCuadreExacto()
    {
        var asiento = new AsientoContable();
        asiento.AgregarDetalle(new AsientoDetalle(1, 100m, 0m, "Debe"));
        asiento.AgregarDetalle(new AsientoDetalle(2, 0m, 100m, "Haber"));

        Assert.True(asiento.EstaCuadrado());
        Assert.Null(Record.Exception(() => asiento.ValidarCuadre()));
    }
}
