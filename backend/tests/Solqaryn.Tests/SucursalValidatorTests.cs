using Solqaryn.Application.DTOs;
using Solqaryn.Application.Validators;
using Xunit;

namespace Solqaryn.Tests;

public class SucursalValidatorTests
{
    [Fact]
    public void CreateSucursal_Valida_NoGeneraErrores()
    {
        var validator = new CreateSucursalValidator();
        var resultado = validator.Validate(new CreateSucursalDto
        {
            EmpresaId = 1,
            Codigo = "TGU-01",
            Nombre = "Sucursal Centro",
            Correo = "centro@example.com",
            ZonaHoraria = "America/Tegucigalpa"
        });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void CreateSucursal_CodigoNombreYEmpresaInvalidos_GeneraErrores()
    {
        var validator = new CreateSucursalValidator();
        var resultado = validator.Validate(new CreateSucursalDto
        {
            EmpresaId = 0,
            Codigo = "",
            Nombre = "",
            ZonaHoraria = ""
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CreateSucursalDto.EmpresaId));
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CreateSucursalDto.Codigo));
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CreateSucursalDto.Nombre));
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CreateSucursalDto.ZonaHoraria));
    }

    [Fact]
    public void FiltroSucursal_FueraDeRango_GeneraErrores()
    {
        var validator = new SucursalFiltroValidator();
        var resultado = validator.Validate(new SucursalFiltroDto
        {
            Pagina = 0,
            TamanoPagina = 101
        });

        Assert.False(resultado.IsValid);
    }
}
