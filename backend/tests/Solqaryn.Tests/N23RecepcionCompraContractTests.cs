using System.ComponentModel.DataAnnotations;
using Solqaryn.Application.DTOs;
using Xunit;

namespace Solqaryn.Tests;

public class N23RecepcionCompraContractTests
{
    [Fact]
    public void CreateDto_SinOrdenCompra_FallaValidacion()
    {
        var dto = new CreateRecepcionCompraDto
        {
            OrdenCompraId = 0,
            Detalles = new List<RecepcionCompraDetalleInputDto> { CrearDetalleValido() }
        };

        var errores = Validar(dto);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(CreateRecepcionCompraDto.OrdenCompraId)));
    }

    [Fact]
    public void CreateDto_SinDetalles_FallaValidacion()
    {
        var dto = new CreateRecepcionCompraDto
        {
            OrdenCompraId = 10,
            Detalles = new List<RecepcionCompraDetalleInputDto>()
        };

        var errores = Validar(dto);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(CreateRecepcionCompraDto.Detalles)));
    }

    [Fact]
    public void DetalleDto_CantidadesNegativas_FallanValidacion()
    {
        var dto = CrearDetalleValido();
        dto.CantidadDanada = -1m;

        var errores = Validar(dto);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraDetalleInputDto.CantidadDanada)));
    }

    [Fact]
    public void DetalleDto_DanadoMasSobranteSuperaRecibido_FallaValidacion()
    {
        var dto = CrearDetalleValido();
        dto.CantidadRecibida = 10m;
        dto.CantidadDanada = 6m;
        dto.CantidadSobrante = 5m;

        var errores = Validar(dto);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraDetalleInputDto.CantidadRecibida)));
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraDetalleInputDto.CantidadDanada)));
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraDetalleInputDto.CantidadSobrante)));
    }

    [Fact]
    public void DetalleDto_SinRecepcionNiFaltante_FallaValidacion()
    {
        var dto = CrearDetalleValido();
        dto.CantidadRecibida = 0m;
        dto.CantidadDanada = 0m;
        dto.CantidadFaltante = 0m;
        dto.CantidadSobrante = 0m;

        var errores = Validar(dto);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraDetalleInputDto.CantidadRecibida)));
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraDetalleInputDto.CantidadFaltante)));
    }

    [Fact]
    public void DetalleDto_ClaveFisicaInvalida_FallaValidacion()
    {
        var dto = CrearDetalleValido();
        dto.AlmacenId = 0;
        dto.UbicacionAlmacenId = 0;

        var errores = Validar(dto);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraDetalleInputDto.AlmacenId)));
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraDetalleInputDto.UbicacionAlmacenId)));
    }

    [Fact]
    public void QueryDto_RangoTemporalInvertido_FallaCerrado()
    {
        var dto = new RecepcionCompraQueryDto
        {
            DesdeUtc = new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            HastaUtc = new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc)
        };

        var errores = Validar(dto);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraQueryDto.DesdeUtc)));
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraQueryDto.HastaUtc)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void QueryDto_PageSizeFueraDeContrato_FallaValidacion(int pageSize)
    {
        var dto = new RecepcionCompraQueryDto { PageSize = pageSize };

        var errores = Validar(dto);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RecepcionCompraQueryDto.PageSize)));
    }

    [Fact]
    public void OutputDto_ExponeCantidadesExplicitasDeRecepcion()
    {
        var detalle = new RecepcionCompraDetalleDto
        {
            CantidadRecibida = 12m,
            CantidadAceptada = 9m,
            CantidadDanada = 1m,
            CantidadFaltante = 0m,
            CantidadSobrante = 2m
        };

        Assert.Equal(12m, detalle.CantidadRecibida);
        Assert.Equal(9m, detalle.CantidadAceptada);
        Assert.Equal(1m, detalle.CantidadDanada);
        Assert.Equal(0m, detalle.CantidadFaltante);
        Assert.Equal(2m, detalle.CantidadSobrante);
    }

    private static RecepcionCompraDetalleInputDto CrearDetalleValido()
    {
        return new RecepcionCompraDetalleInputDto
        {
            OrdenCompraDetalleId = 100,
            AlmacenId = 400,
            UbicacionAlmacenId = 500,
            CantidadRecibida = 5m,
            CantidadDanada = 1m,
            CantidadFaltante = 0m,
            CantidadSobrante = 0m
        };
    }

    private static List<ValidationResult> Validar(object value)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), resultados, validateAllProperties: true);
        return resultados;
    }
}