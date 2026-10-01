using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N17ConteoInventarioContractTests
{
    [Fact]
    public void Enums_MantienenValoresEstablesDelLifecycleYTipos()
    {
        Assert.Equal(1, (int)EstadoConteoInventario.Borrador);
        Assert.Equal(2, (int)EstadoConteoInventario.EnProceso);
        Assert.Equal(3, (int)EstadoConteoInventario.Cerrado);
        Assert.Equal(4, (int)EstadoConteoInventario.Aprobado);
        Assert.Equal(5, (int)EstadoConteoInventario.Cancelado);

        Assert.Equal(1, (int)TipoConteoInventario.General);
        Assert.Equal(2, (int)TipoConteoInventario.Ciclico);
        Assert.Equal(3, (int)TipoConteoInventario.PorUbicacion);
        Assert.Equal(4, (int)TipoConteoInventario.PorCategoria);
        Assert.Equal(5, (int)TipoConteoInventario.Ciego);
    }

    [Fact]
    public void CapturaDeLinea_NoExponeStockEsperadoNiDiferencia()
    {
        var propiedades = typeof(CapturarConteoInventarioDetalleDto)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();

        Assert.Contains(nameof(CapturarConteoInventarioDetalleDto.CantidadContada), propiedades);
        Assert.DoesNotContain("StockEsperado", propiedades);
        Assert.DoesNotContain("StockEsperadoSnapshot", propiedades);
        Assert.DoesNotContain("Diferencia", propiedades);
    }

    [Fact]
    public void CapturaPorLote_SoloIdentificaLineaYCantidadObservada()
    {
        var propiedades = typeof(CapturaConteoInventarioLineaDto)
            .GetProperties()
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(new[] { "CantidadContada", "DetalleId" }, propiedades);
    }

    [Fact]
    public void CrearConteo_ExponeScopeEmpresarialSinCampoDeStockAutoritativo()
    {
        var propiedades = typeof(CreateConteoInventarioDto)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();

        Assert.Contains(nameof(CreateConteoInventarioDto.AlmacenId), propiedades);
        Assert.Contains(nameof(CreateConteoInventarioDto.UbicacionAlmacenId), propiedades);
        Assert.Contains(nameof(CreateConteoInventarioDto.CategoriaId), propiedades);
        Assert.Contains(nameof(CreateConteoInventarioDto.ProductoVarianteIds), propiedades);
        Assert.Contains(nameof(CreateConteoInventarioDto.EsCiego), propiedades);
        Assert.DoesNotContain("StockFisico", propiedades);
        Assert.DoesNotContain("StockDisponible", propiedades);
    }

    [Fact]
    public void Resumen_DerivaSiPuedeCerrarDesdePendientes()
    {
        var pendiente = new ConteoInventarioResumenDto
        {
            TotalLineas = 3,
            Capturadas = 2,
            Pendientes = 1
        };
        var completo = new ConteoInventarioResumenDto
        {
            TotalLineas = 3,
            Capturadas = 3,
            Pendientes = 0
        };

        Assert.False(pendiente.PuedeCerrar);
        Assert.True(completo.PuedeCerrar);
    }
}
