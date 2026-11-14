using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Services;

public class ThreeWayMatchServiceTests
{
    private readonly Mock<IOrdenCompraRepository> _mockOcRepo;
    private readonly Mock<IRecepcionCompraRepository> _mockRcRepo;
    private readonly Mock<IFacturaProveedorRepository> _mockFpRepo;
    private readonly ThreeWayMatchService _service;

    public ThreeWayMatchServiceTests()
    {
        _mockOcRepo = new Mock<IOrdenCompraRepository>();
        _mockRcRepo = new Mock<IRecepcionCompraRepository>();
        _mockFpRepo = new Mock<IFacturaProveedorRepository>();

        _service = new ThreeWayMatchService(
            _mockOcRepo.Object,
            _mockRcRepo.Object,
            _mockFpRepo.Object);
    }

    [Fact]
    public async Task EvaluarAsync_OrdenInexistente_LanzaResourceNotFoundException()
    {
        _mockOcRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync((OrdenCompra?)null);

        var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _service.EvaluarAsync(1, CancellationToken.None));

        Assert.Contains("No existe la orden de compra 1", ex.Message);
    }

    [Fact]
    public async Task EvaluarAsync_CancellationTokenCancelado_LanzaOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.EvaluarAsync(1, cts.Token));
    }

    [Fact]
    public async Task EvaluarAsync_MapeoRealDeDiscrepancias()
    {
        var oc = new OrdenCompra { Id = 1, Moneda = "USD", NumeroOrden = "OC-1", ProveedorId = 1 };
        var ocDetalle = new OrdenCompraDetalle { Id = 10, OrdenCompraId = 1, ProductoId = 1 };
        ocDetalle.EstablecerValores(10m, 100m, 0m, 0m);
        oc.Detalles.Add(ocDetalle);
        _mockOcRepo.Setup(x => x.GetByIdAsync(1, false)).ReturnsAsync(oc);

        var rc = new RecepcionCompra { Id = 1, OrdenCompraId = 1, NumeroRecepcion = "RC-1" };
        var rcDetalle = new RecepcionCompraDetalle { Id = 1, OrdenCompraDetalleId = 10, ProductoId = 1, AlmacenId = 1 };
        rcDetalle.EstablecerCantidades(8m);
        rc.Detalles.Add(rcDetalle);
        rc.Confirmar(1, "Tester", DateTime.UtcNow);
        _mockRcRepo.Setup(x => x.GetPagedAsync(It.IsAny<RecepcionCompraQueryDto>()))
            .ReturnsAsync((new List<RecepcionCompra> { rc }, 1));

        var fp = new FacturaProveedor
        {
            Id = 1,
            OrdenCompraId = 1,
            ProveedorId = 1,
            NumeroFactura = "FP-1",
            ProveedorNombreSnapshot = "Prov",
            FechaEmisionUtc = DateTime.UtcNow,
            Moneda = "USD"
        };
        var fpDetalle = new FacturaProveedorDetalle
        {
            Id = 1,
            OrdenCompraDetalleId = 10,
            ProductoId = 1,
            ProductoNombreSnapshot = "Prod"
        };
        fpDetalle.EstablecerValores(8m, 105m, 0m, 0m);
        fp.Detalles.Add(fpDetalle);
        fp.Registrar(1, "Tester", DateTime.UtcNow);
        _mockFpRepo.Setup(x => x.GetPagedAsync(It.IsAny<FacturaProveedorFiltroDto>()))
            .ReturnsAsync((new List<FacturaProveedor> { fp }, 1));

        var result = await _service.EvaluarAsync(1);

        Assert.Equal(ThreeWayMatchStatus.Discrepancia, result.Estado);
        Assert.Equal(2, result.Discrepancias.Count);

        var cantidad = Assert.Single(result.Discrepancias.Where(d => d.Tipo == ThreeWayMatchDiscrepancyType.Cantidad));
        Assert.Equal(10, cantidad.OrdenCompraDetalleId);
        Assert.Equal(10m, cantidad.EsperadoOrdenado);
        Assert.Equal(8m, cantidad.ValorRecepcion);
        Assert.Equal(8m, cantidad.ValorFacturado);

        var precio = Assert.Single(result.Discrepancias.Where(d => d.Tipo == ThreeWayMatchDiscrepancyType.Precio));
        Assert.Equal(100m, precio.EsperadoOrdenado);
        Assert.Equal(105m, precio.ValorFacturado);
    }

    [Fact]
    public async Task EvaluarAsync_EvidenciaInestable_RecepcionesCambianTotal_LanzaException()
    {
        var oc = new OrdenCompra { Id = 1, Moneda = "HNL", NumeroOrden = "OC-1", ProveedorId = 1 };
        _mockOcRepo.Setup(x => x.GetByIdAsync(1, false)).ReturnsAsync(oc);

        _mockRcRepo.SetupSequence(x => x.GetPagedAsync(It.IsAny<RecepcionCompraQueryDto>()))
            .ReturnsAsync((new List<RecepcionCompra> { new() { Id = 1, OrdenCompraId = 1, NumeroRecepcion = "RC-1" } }, 2))
            .ReturnsAsync((new List<RecepcionCompra> { new() { Id = 2, OrdenCompraId = 1, NumeroRecepcion = "RC-2" } }, 3));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.EvaluarAsync(1));
        Assert.Contains("La evidencia de recepciones cambió durante la conciliación", ex.Message);
    }

    [Fact]
    public async Task EvaluarAsync_EvidenciaInestable_FacturasPaginaIncompleta_LanzaException()
    {
        var oc = new OrdenCompra { Id = 1, Moneda = "HNL", NumeroOrden = "OC-1", ProveedorId = 1 };
        _mockOcRepo.Setup(x => x.GetByIdAsync(1, false)).ReturnsAsync(oc);
        _mockRcRepo.Setup(x => x.GetPagedAsync(It.IsAny<RecepcionCompraQueryDto>()))
            .ReturnsAsync((new List<RecepcionCompra>(), 0));

        _mockFpRepo.SetupSequence(x => x.GetPagedAsync(It.IsAny<FacturaProveedorFiltroDto>()))
            .ReturnsAsync((new List<FacturaProveedor> { new() { Id = 1, OrdenCompraId = 1, NumeroFactura = "FP-1" } }, 2))
            .ReturnsAsync((new List<FacturaProveedor>(), 2));

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.EvaluarAsync(1));
        Assert.Contains("La evidencia de facturas de proveedor cambió durante la conciliación", ex.Message);
    }

    [Fact]
    public async Task EvaluarAsync_MasDe100Registros_PaginaCompletaSinPerderEvidencia()
    {
        const int total = 150;
        var oc = new OrdenCompra { Id = 1, Moneda = "HNL", NumeroOrden = "OC-1", ProveedorId = 1 };
        var ocDetalle = new OrdenCompraDetalle { Id = 10, OrdenCompraId = 1, ProductoId = 1 };
        ocDetalle.EstablecerValores(total, 100m, 0m, 0m);
        oc.Detalles.Add(ocDetalle);
        _mockOcRepo.Setup(x => x.GetByIdAsync(1, false)).ReturnsAsync(oc);

        static RecepcionCompra CrearRecepcion(int id)
        {
            var recepcion = new RecepcionCompra { Id = id, OrdenCompraId = 1, NumeroRecepcion = $"RC-{id}" };
            var detalle = new RecepcionCompraDetalle { Id = id, OrdenCompraDetalleId = 10, ProductoId = 1, AlmacenId = 1 };
            detalle.EstablecerCantidades(1m);
            recepcion.Detalles.Add(detalle);
            recepcion.Confirmar(1, "Tester", DateTime.UtcNow);
            return recepcion;
        }

        static FacturaProveedor CrearFactura(int id)
        {
            var factura = new FacturaProveedor
            {
                Id = id,
                OrdenCompraId = 1,
                ProveedorId = 1,
                NumeroFactura = $"FP-{id}",
                ProveedorNombreSnapshot = "Prov",
                FechaEmisionUtc = DateTime.UtcNow,
                Moneda = "HNL"
            };
            var detalle = new FacturaProveedorDetalle
            {
                Id = id,
                OrdenCompraDetalleId = 10,
                ProductoId = 1,
                ProductoNombreSnapshot = "Prod"
            };
            detalle.EstablecerValores(1m, 100m, 0m, 0m);
            factura.Detalles.Add(detalle);
            factura.Registrar(1, "Tester", DateTime.UtcNow);
            return factura;
        }

        var recepciones = Enumerable.Range(1, total).Select(CrearRecepcion).ToList();
        var facturas = Enumerable.Range(1, total).Select(CrearFactura).ToList();

        _mockRcRepo.SetupSequence(x => x.GetPagedAsync(It.IsAny<RecepcionCompraQueryDto>()))
            .ReturnsAsync((recepciones.Take(100).ToList(), total))
            .ReturnsAsync((recepciones.Skip(100).ToList(), total));
        _mockFpRepo.SetupSequence(x => x.GetPagedAsync(It.IsAny<FacturaProveedorFiltroDto>()))
            .ReturnsAsync((facturas.Take(100).ToList(), total))
            .ReturnsAsync((facturas.Skip(100).ToList(), total));

        var result = await _service.EvaluarAsync(1);

        Assert.Equal(ThreeWayMatchStatus.Aprobado, result.Estado);
        Assert.Empty(result.Discrepancias);
        _mockRcRepo.Verify(x => x.GetPagedAsync(It.IsAny<RecepcionCompraQueryDto>()), Times.Exactly(2));
        _mockFpRepo.Verify(x => x.GetPagedAsync(It.IsAny<FacturaProveedorFiltroDto>()), Times.Exactly(2));
    }
}
