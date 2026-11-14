using System;
using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public sealed class PeriodoContableRetroactivePolicyTests
{
    [Fact]
    public void ValidarCambio_RechazaPeriodoCerradoSinAutorizacion()
    {
        var periodo = new PeriodoContable(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc));
        periodo.Cerrar(new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var fechaOperacion = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var exception = Assert.Throws<InvalidOperationException>(() => periodo.ValidarCambio(fechaOperacion, false));
        Assert.Equal("El período contable está cerrado; el cambio retroactivo requiere autorización explícita.", exception.Message);
    }

    [Fact]
    public void ValidarCambio_PermitePeriodoCerradoConAutorizacion()
    {
        var periodo = new PeriodoContable(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc));
        periodo.Cerrar(new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        periodo.ValidarCambio(new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc), true);
    }

    [Fact]
    public void ValidarCambio_PermitePeriodoAbiertoSinAutorizacion()
    {
        var periodo = new PeriodoContable(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc));
        periodo.ValidarCambio(new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc), false);
    }

    [Fact]
    public void ValidarCambio_RechazaOperacionFueraDelPeriodo()
    {
        var periodo = new PeriodoContable(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc));

        var before = Assert.Throws<InvalidOperationException>(() => periodo.ValidarCambio(new DateTime(2022, 12, 31, 23, 59, 59, DateTimeKind.Utc), false));
        var after = Assert.Throws<InvalidOperationException>(() => periodo.ValidarCambio(new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc), false));

        Assert.Equal("La operación está fuera del período contable.", before.Message);
        Assert.Equal("La operación está fuera del período contable.", after.Message);
    }
}
