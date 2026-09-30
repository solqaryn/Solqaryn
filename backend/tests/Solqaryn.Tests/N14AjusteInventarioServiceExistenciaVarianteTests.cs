using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N14AjusteInventarioServiceExistenciaVarianteTests
{
    [Fact]
    public void CrearDemanda_DebeMapearLaClaveFisicaAutoritativa()
    {
        var detalle = new AjusteInventarioDetalle
        {
            ProductoId = 10,
            ProductoVarianteId = 20,
            AlmacenId = 30,
            UbicacionAlmacenId = 40,
            CantidadObjetivo = 7
        };

        var demanda = AjusteInventarioExistenciaContext.CrearDemanda(detalle);

        Assert.Equal(10, demanda.ProductoId);
        Assert.Equal(20, demanda.ProductoVarianteId);
        Assert.Equal(30, demanda.AlmacenId);
        Assert.Equal(40, demanda.UbicacionAlmacenId);
        Assert.Equal(1, demanda.Cantidad);
        Assert.Equal(20, demanda.Clave.ProductoVarianteId);
        Assert.Equal(30, demanda.Clave.AlmacenId);
        Assert.Equal(40, demanda.Clave.UbicacionAlmacenId);
    }

    [Fact]
    public void CrearDemanda_DebeFallarCerradoSinVarianteOAlmacen()
    {
        var sinVariante = new AjusteInventarioDetalle
        {
            ProductoId = 10,
            AlmacenId = 30,
            CantidadObjetivo = 7
        };
        var sinAlmacen = new AjusteInventarioDetalle
        {
            ProductoId = 10,
            ProductoVarianteId = 20,
            CantidadObjetivo = 7
        };

        Assert.Throws<BusinessRuleException>(() => AjusteInventarioExistenciaContext.CrearDemanda(sinVariante));
        Assert.Throws<BusinessRuleException>(() => AjusteInventarioExistenciaContext.CrearDemanda(sinAlmacen));
    }
}
