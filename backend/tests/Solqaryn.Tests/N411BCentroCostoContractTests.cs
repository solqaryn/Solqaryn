using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Validators;
using Solqaryn.Domain.Entities.Contabilidad;
using Xunit;

namespace Solqaryn.Tests;

public class N411BCentroCostoContractTests
{
    [Fact]
    public void Create_Sucursal_Con_SucursalId_Valido_Aprueba()
    {
        var validator = new CreateCentroCostoValidator();
        var dto = new CreateCentroCostoDto
        {
            Codigo = "CC-TGU",
            Nombre = "Centro de costo Tegucigalpa",
            Tipo = TipoCentroCosto.Sucursal,
            SucursalId = 1
        };

        Assert.True(validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Create_Sucursal_Sin_SucursalId_Falla_Cerrado()
    {
        var validator = new CreateCentroCostoValidator();
        var dto = new CreateCentroCostoDto
        {
            Codigo = "CC-TGU",
            Nombre = "Centro de costo Tegucigalpa",
            Tipo = TipoCentroCosto.Sucursal,
            SucursalId = null
        };

        Assert.False(validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Create_NoSucursal_Con_SucursalId_Falla_Cerrado()
    {
        var validator = new CreateCentroCostoValidator();
        var dto = new CreateCentroCostoDto
        {
            Codigo = "CC-PROY",
            Nombre = "Centro de costo proyecto",
            Tipo = TipoCentroCosto.Proyecto,
            SucursalId = 1
        };

        Assert.False(validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Create_Tipo_Fuera_De_Enum_Falla()
    {
        var validator = new CreateCentroCostoValidator();
        var dto = new CreateCentroCostoDto
        {
            Codigo = "CC-X",
            Nombre = "Centro inválido",
            Tipo = (TipoCentroCosto)999,
            SucursalId = null
        };

        Assert.False(validator.Validate(dto).IsValid);
    }

    [Fact]
    public void Repository_Contract_No_Expone_Implementacion_De_Persistencia()
    {
        Assert.True(typeof(ICentroCostoRepository).IsInterface);
        Assert.Contains(typeof(ICentroCostoRepository).GetMethods(), method => method.Name == "GetByIdAsync");
        Assert.Contains(typeof(ICentroCostoRepository).GetMethods(), method => method.Name == "ExisteCodigoAsync");
        Assert.Contains(typeof(ICentroCostoRepository).GetMethods(), method => method.Name == "SaveChangesAsync");
    }
}
