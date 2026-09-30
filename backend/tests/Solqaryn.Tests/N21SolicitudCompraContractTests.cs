using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N21SolicitudCompraContractTests
{
    [Fact]
    public void EstadoSolicitudCompra_MantieneContratoEstableIndependienteDeEstadoDocumento()
    {
        Assert.Equal(1, (int)EstadoSolicitudCompra.Borrador);
        Assert.Equal(2, (int)EstadoSolicitudCompra.Solicitada);
        Assert.Equal(3, (int)EstadoSolicitudCompra.Aprobada);
        Assert.Equal(4, (int)EstadoSolicitudCompra.Rechazada);
        Assert.DoesNotContain(typeof(EstadoSolicitudCompra), typeof(EstadoDocumento).GetInterfaces());
    }

    [Fact]
    public void CreateDto_PermiteProveedorOpcionalYDetallePorProductoOVariante()
    {
        var dto = new CreateSolicitudCompraDto
        {
            ProveedorId = null,
            Notas = "Reposición preventiva",
            Detalles =
            {
                new SolicitudCompraDetalleInputDto
                {
                    ProductoId = 10,
                    ProductoVarianteId = 20,
                    CantidadSolicitada = 4,
                    CostoEstimadoUnitario = 125.50m
                }
            }
        };

        var detalle = Assert.Single(dto.Detalles);
        Assert.Null(dto.ProveedorId);
        Assert.Equal(10, detalle.ProductoId);
        Assert.Equal(20, detalle.ProductoVarianteId);
        Assert.Equal(4, detalle.CantidadSolicitada);
        Assert.Equal(125.50m, detalle.CostoEstimadoUnitario);
    }

    [Fact]
    public void Filtro_PreservaPaginacionYDimensionesDocumentales()
    {
        var filtro = new SolicitudCompraFiltroDto
        {
            Estado = EstadoSolicitudCompra.Solicitada,
            ProveedorId = 7,
            Numero = "SC-2026",
            Desde = DateTime.UtcNow.AddDays(-7),
            Hasta = DateTime.UtcNow
        };

        Assert.Equal("FechaCreacion", filtro.SortBy);
        Assert.Equal("desc", filtro.SortDirection);
        Assert.Equal(EstadoSolicitudCompra.Solicitada, filtro.Estado);
        Assert.Equal(7, filtro.ProveedorId);
    }

    [Fact]
    public void Dominio_NoHeredaContratoTransaccionalConfirmable()
    {
        Assert.Equal("AuditableEntity", typeof(SolicitudCompra).BaseType?.Name);
        Assert.NotEqual("ConfirmableEntity", typeof(SolicitudCompra).BaseType?.Name);
    }

    [Fact]
    public void Solicitar_NoExponeCamposDeInventarioKardexOCosteoEnAgregado()
    {
        var nombres = typeof(SolicitudCompra)
            .GetProperties()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("Stock", nombres);
        Assert.DoesNotContain("Kardex", nombres);
        Assert.DoesNotContain("CostoPromedio", nombres);
        Assert.DoesNotContain("MovimientoFinanciero", nombres);
    }
}
