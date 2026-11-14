using FluentValidation.TestHelper;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Validators;
using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public class N411DCentrosCostoValidationTests
{
    private readonly CreateCentroCostoValidator _createValidator = new();
    private readonly UpdateCentroCostoValidator _updateValidator = new();

    [Fact]
    public void CreateValidator_RechazaCodigoVacio()
    {
        var result = _createValidator.TestValidate(new CreateCentroCostoDto { Codigo = "", Nombre = "Test", Tipo = TipoCentroCosto.Departamento });
        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Fact]
    public void CreateValidator_AceptaCodigoValido()
    {
        var result = _createValidator.TestValidate(new CreateCentroCostoDto { Codigo = "CC-001", Nombre = "Test", Tipo = TipoCentroCosto.Departamento });
        result.ShouldNotHaveValidationErrorFor(x => x.Codigo);
    }

    [Fact]
    public void CreateValidator_RechazaSucursalSinSucursalId()
    {
        var result = _createValidator.TestValidate(new CreateCentroCostoDto { Tipo = TipoCentroCosto.Sucursal, SucursalId = null, Codigo = "CC-01", Nombre = "Nombre" });
        result.ShouldHaveValidationErrorFor(x => x.SucursalId);
    }

    [Fact]
    public void CreateValidator_RechazaSucursalIdEnTipoNoSucursal()
    {
        var result = _createValidator.TestValidate(new CreateCentroCostoDto { Tipo = TipoCentroCosto.Departamento, SucursalId = 1, Codigo = "CC-01", Nombre = "Nombre" });
        result.ShouldHaveValidationErrorFor(x => x.SucursalId);
    }

    [Fact]
    public void UpdateValidator_RechazaAsociacionesInconsistentes()
    {
        var missing = _updateValidator.TestValidate(new UpdateCentroCostoDto { Tipo = TipoCentroCosto.Sucursal, SucursalId = null, Codigo = "CC-01", Nombre = "Nombre" });
        missing.ShouldHaveValidationErrorFor(x => x.SucursalId);

        var unexpected = _updateValidator.TestValidate(new UpdateCentroCostoDto { Tipo = TipoCentroCosto.Departamento, SucursalId = 1, Codigo = "CC-01", Nombre = "Nombre" });
        unexpected.ShouldHaveValidationErrorFor(x => x.SucursalId);
    }
}
