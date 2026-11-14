using System;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N29EvaluacionProveedorDomainTests
{
    [Fact]
    public void Constructor_SetsProperties_Correctly()
    {
        var fechaEsperadaUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var fechaRecepcionUtc = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);

        var evaluacion = new EvaluacionProveedor(1, 2, 3, fechaEsperadaUtc, fechaRecepcionUtc);

        Assert.Equal(1, evaluacion.ProveedorId);
        Assert.Equal(2, evaluacion.OrdenCompraId);
        Assert.Equal(3, evaluacion.RecepcionCompraId);
        Assert.Equal(fechaEsperadaUtc, evaluacion.FechaEsperadaUtc);
        Assert.Equal(fechaRecepcionUtc, evaluacion.FechaRecepcionUtc);
    }

    [Fact]
    public void EstablecerCantidades_ValidValues_SetsQuantities()
    {
        var evaluacion = new EvaluacionProveedor(1, 2, 3, DateTime.UtcNow, DateTime.UtcNow);
        evaluacion.EstablecerCantidades(10m, 10m, 8m, 1m, 1m);

        Assert.Equal(10m, evaluacion.CantidadOrdenada);
        Assert.Equal(8m, evaluacion.CantidadAceptada);
        Assert.Equal(1m, evaluacion.CantidadDanada);
        Assert.Equal(1m, evaluacion.CantidadSobrante);
    }

    [Fact]
    public void EstablecerCantidades_IncoherentQuantities_ThrowsException()
    {
        var evaluacion = new EvaluacionProveedor(1, 2, 3, DateTime.UtcNow, DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => evaluacion.EstablecerCantidades(10m, 10m, 7m, 1m, 1m));
        Assert.Equal("La cantidad aceptada no es coherente con la recibida, dañada y sobrante.", exception.Message);
    }

    [Fact]
    public void EstablecerCantidades_NegativeQuantities_ThrowsException()
    {
        var evaluacion = new EvaluacionProveedor(1, 2, 3, DateTime.UtcNow, DateTime.UtcNow);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => evaluacion.EstablecerCantidades(10m, 10m, -1m, 5m, 6m));
        Assert.Contains("Las cantidades no pueden ser negativas", exception.Message);
    }
}
