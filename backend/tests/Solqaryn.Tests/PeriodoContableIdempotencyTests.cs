using System;
using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public sealed class PeriodoContableIdempotencyTests
{
    [Fact]
    public void PeriodoContable_Creacion_RequiereFechasConZonaHoraria_GarantizaEstadoInicial()
    {
        var fechaInicio = DateTime.UtcNow;
        var fechaFin = DateTime.UtcNow.AddDays(30);
        var periodo = new PeriodoContable(fechaInicio, fechaFin);

        Assert.Equal(fechaInicio, periodo.FechaInicio);
        Assert.Equal(fechaFin, periodo.FechaFin);
        Assert.Equal(EstadoPeriodoContable.Abierto, periodo.Estado);
        Assert.Null(periodo.CerradoEnUtc);
    }

    [Fact]
    public void PeriodoContable_Cierre_Reintento_Rechazado_SinAlterarEstado()
    {
        var periodo = new PeriodoContable(DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        var cerradoEnUtc = DateTime.UtcNow;
        periodo.Cerrar(cerradoEnUtc);

        var exception = Assert.Throws<InvalidOperationException>(() => periodo.Cerrar(DateTime.UtcNow.AddDays(1)));

        Assert.Equal("El período contable ya está cerrado.", exception.Message);
        Assert.Equal(EstadoPeriodoContable.Cerrado, periodo.Estado);
        Assert.Equal(cerradoEnUtc, periodo.CerradoEnUtc);
    }

    [Fact]
    public void PeriodoContable_FechasSinZonaHoraria_NoPermitidas()
    {
        var fechaInicio = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var fechaFin = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Unspecified);

        var exception = Assert.Throws<ArgumentException>(() => new PeriodoContable(fechaInicio, fechaFin));
        Assert.Equal("Las fechas del período contable deben tener zona horaria explícita.", exception.Message);
    }

    [Fact]
    public void PeriodoContable_Cierre_SinUtc_NoPermitido()
    {
        var periodo = new PeriodoContable(DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        var fechaCierreLocal = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Local);

        var exception = Assert.Throws<ArgumentException>(() => periodo.Cerrar(fechaCierreLocal));
        Assert.StartsWith("La fecha de cierre debe expresarse en UTC.", exception.Message);
    }
}
