using FluentValidation.TestHelper;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Validators;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioValidatorTests
{
    [Fact]
    public void Create_RechazaMismoAlmacenYDetalleDuplicado()
    {
        var validator = new CreateTransferenciaInventarioValidator();
        var dto = new CreateTransferenciaInventarioDto
        {
            AlmacenOrigenId = 3,
            AlmacenDestinoId = 3,
            Detalles =
            {
                new TransferenciaInventarioDetalleInputDto { ProductoVarianteId = 7, CantidadSolicitada = 2 },
                new TransferenciaInventarioDetalleInputDto { ProductoVarianteId = 7, CantidadSolicitada = 1 }
            }
        };

        var result = validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AlmacenDestinoId);
        result.ShouldHaveValidationErrorFor(x => x.Detalles);
    }

    [Fact]
    public void Recibir_AceptaRecepcionConFaltanteDanadaYSobranteNoNegativos()
    {
        var validator = new RecibirTransferenciaInventarioValidator();
        var dto = new RecibirTransferenciaInventarioDto
        {
            Detalles =
            {
                new RecibirTransferenciaInventarioDetalleDto
                {
                    DetalleId = 10,
                    CantidadRecibida = 5,
                    CantidadFaltante = 1,
                    CantidadDanada = 1,
                    CantidadSobrante = 2
                }
            }
        };

        var result = validator.TestValidate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Filtro_RechazaRangoTemporalInvertido()
    {
        var validator = new TransferenciaInventarioFiltroValidator();
        var dto = new TransferenciaInventarioFiltroDto
        {
            Desde = new DateTime(2026, 8, 16),
            Hasta = new DateTime(2026, 8, 15)
        };

        validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Hasta);
    }

    [Fact]
    public void Cancelar_ExigeMotivo()
    {
        var validator = new CancelarTransferenciaInventarioValidator();

        validator.TestValidate(new CancelarTransferenciaInventarioDto())
            .ShouldHaveValidationErrorFor(x => x.Motivo);
    }
}
