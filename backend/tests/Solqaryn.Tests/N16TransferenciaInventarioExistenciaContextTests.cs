using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioExistenciaContextTests
{
    [Fact]
    public void ConstruirDemandasDespacho_UsaClaveFisicaOrigenYCantidadDespachada()
    {
        var transferencia = CrearTransferencia();
        var detalle = transferencia.Detalles.Single();
        detalle.EstablecerCantidadSolicitada(8);
        detalle.AprobarCantidad(6);
        detalle.RegistrarDespacho(5);

        var demandas = TransferenciaInventarioExistenciaContext.ConstruirDemandasDespacho(transferencia);

        var demanda = Assert.Single(demandas);
        Assert.Equal(44, demanda.ProductoId);
        Assert.Equal(91, demanda.ProductoVarianteId);
        Assert.Equal(10, demanda.AlmacenId);
        Assert.Equal(101, demanda.UbicacionAlmacenId);
        Assert.Equal(5, demanda.Cantidad);
    }

    [Fact]
    public void ConstruirDemandasDespacho_FallaCerrado_SiVarianteNoEstaCargada()
    {
        var transferencia = CrearTransferencia();
        var detalle = transferencia.Detalles.Single();
        detalle.ProductoVariante = null!;
        detalle.EstablecerCantidadSolicitada(2);
        detalle.AprobarCantidad(2);
        detalle.RegistrarDespacho(2);

        var ex = Assert.Throws<BusinessRuleException>(
            () => TransferenciaInventarioExistenciaContext.ConstruirDemandasDespacho(transferencia));

        Assert.Contains("variante cargada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConstruirDemandasDespacho_FallaCerrado_SiCantidadNoFueDespachada()
    {
        var transferencia = CrearTransferencia();

        var ex = Assert.Throws<BusinessRuleException>(
            () => TransferenciaInventarioExistenciaContext.ConstruirDemandasDespacho(transferencia));

        Assert.Contains("cantidad despachada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static TransferenciaInventario CrearTransferencia()
    {
        var variante = new ProductoVariante
        {
            Id = 91,
            ProductoId = 44,
            Sku = "SKU-N16",
            Activo = true
        };
        return new TransferenciaInventario
        {
            Id = 31,
            Numero = "TRF-N16",
            AlmacenOrigenId = 10,
            AlmacenDestinoId = 20,
            CreadoPorUsuarioId = 7,
            Detalles = new List<TransferenciaInventarioDetalle>
            {
                new()
                {
                    Id = 301,
                    ProductoVarianteId = variante.Id,
                    ProductoVariante = variante,
                    UbicacionOrigenId = 101,
                    UbicacionDestinoId = 202,
                    CreadoPorUsuarioId = 7
                }
            }
        };
    }
}
