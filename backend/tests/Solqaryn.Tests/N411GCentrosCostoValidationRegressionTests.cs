using FluentValidation.TestHelper;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Validators;
using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N411GCentrosCostoValidationRegressionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RechazaCodigoVacioOEnBlanco(string codigo)
    {
        var result = new CreateCentroCostoValidator().TestValidate(new CreateCentroCostoDto
        {
            Codigo = codigo,
            Nombre = "Administracion",
            Tipo = TipoCentroCosto.Departamento
        });

        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Fact]
    public void Create_RechazaLongitudesFueraDeContrato()
    {
        var result = new CreateCentroCostoValidator().TestValidate(new CreateCentroCostoDto
        {
            Codigo = new string('C', 41),
            Nombre = new string('N', 151),
            Descripcion = new string('D', 501),
            Tipo = TipoCentroCosto.Departamento
        });

        result.ShouldHaveValidationErrorFor(x => x.Codigo);
        result.ShouldHaveValidationErrorFor(x => x.Nombre);
        result.ShouldHaveValidationErrorFor(x => x.Descripcion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_Sucursal_RechazaSucursalIdNoPositivo(int sucursalId)
    {
        var result = new UpdateCentroCostoValidator().TestValidate(new UpdateCentroCostoDto
        {
            Codigo = "CC-001",
            Nombre = "Administracion",
            Tipo = TipoCentroCosto.Sucursal,
            SucursalId = sucursalId
        });

        result.ShouldHaveValidationErrorFor(x => x.SucursalId);
    }

    [Fact]
    public void AmbosContratos_RechazanTipoFueraDelEnum()
    {
        var create = new CreateCentroCostoValidator().TestValidate(new CreateCentroCostoDto
        {
            Codigo = "CC-001",
            Nombre = "Administracion",
            Tipo = (TipoCentroCosto)999
        });
        var update = new UpdateCentroCostoValidator().TestValidate(new UpdateCentroCostoDto
        {
            Codigo = "CC-001",
            Nombre = "Administracion",
            Tipo = (TipoCentroCosto)999
        });

        create.ShouldHaveValidationErrorFor(x => x.Tipo);
        update.ShouldHaveValidationErrorFor(x => x.Tipo);
    }
}
