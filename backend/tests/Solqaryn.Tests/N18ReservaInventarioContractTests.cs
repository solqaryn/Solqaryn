using System.ComponentModel.DataAnnotations;
using Solqaryn.Application.DTOs;
using Xunit;

namespace Solqaryn.Tests;

public class N18ReservaInventarioContractTests
{
    [Fact]
    public void Detalle_input_exige_variante_almacen_y_cantidad_positivos()
    {
        var dto = new ReservaInventarioDetalleInputDto
        {
            ProductoVarianteId = 0,
            AlmacenId = 0,
            UbicacionAlmacenId = 0,
            Cantidad = 0
        };

        var errores = Validar(dto);

        Assert.True(errores.Count >= 4);
    }

    [Fact]
    public void Crear_reserva_rechaza_lista_vacia()
    {
        var dto = new CreateReservaInventarioDto
        {
            VentaId = 31,
            Detalles = new List<ReservaInventarioDetalleInputDto>()
        };

        var errores = Validar(dto);

        Assert.Contains(errores, x => x.MemberNames.Contains(nameof(CreateReservaInventarioDto.Detalles)));
    }

    [Fact]
    public void Crear_reserva_admite_existencia_raiz_sin_ubicacion()
    {
        var dto = new ReservaInventarioDetalleInputDto
        {
            ProductoVarianteId = 11,
            AlmacenId = 5,
            UbicacionAlmacenId = null,
            Cantidad = 2
        };

        Assert.Empty(Validar(dto));
    }

    [Fact]
    public void Query_limita_page_size_a_100_por_contrato()
    {
        var dto = new ReservaInventarioQueryDto { Page = 1, PageSize = 101 };

        var errores = Validar(dto);

        Assert.Contains(errores, x => x.MemberNames.Contains(nameof(ReservaInventarioQueryDto.PageSize)));
    }

    [Fact]
    public void Query_rechaza_rango_de_expiracion_invertido()
    {
        var dto = new ReservaInventarioQueryDto
        {
            ExpiraDesde = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc),
            ExpiraHasta = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc)
        };

        var errores = Validar(dto);

        Assert.Contains(errores, x =>
            x.MemberNames.Contains(nameof(ReservaInventarioQueryDto.ExpiraDesde)) &&
            x.MemberNames.Contains(nameof(ReservaInventarioQueryDto.ExpiraHasta)));
    }

    [Fact]
    public void Query_admite_rango_de_expiracion_ordenado()
    {
        var dto = new ReservaInventarioQueryDto
        {
            ExpiraDesde = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc),
            ExpiraHasta = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.Empty(Validar(dto));
    }

    [Fact]
    public void Liberar_y_cancelar_exigen_motivo_no_vacio()
    {
        Assert.NotEmpty(Validar(new LiberarReservaInventarioDto { Motivo = string.Empty }));
        Assert.NotEmpty(Validar(new CancelarReservaInventarioDto { Motivo = string.Empty }));
    }

    private static IReadOnlyList<ValidationResult> Validar(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            results,
            validateAllProperties: true);
        return results;
    }
}
