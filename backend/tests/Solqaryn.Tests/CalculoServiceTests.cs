using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class CalculoServiceTests
{
    private readonly Mock<IDescuentoRepository> _descuentoRepository = new();
    private readonly Mock<IImpuestoRepository> _impuestoRepository = new();
    private readonly Mock<ICostoEnvioRepository> _costoEnvioRepository = new();
    private readonly CalculoService _service;

    public CalculoServiceTests()
    {
        _descuentoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Descuento>());

        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), It.IsAny<OperacionImpuesto>()))
            .ReturnsAsync(new List<Impuesto>());

        _costoEnvioRepository
            .Setup(r => r.GetPredeterminadoVigenteAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new CostoEnvio
            {
                Id = 1,
                Nombre = "Sin costo",
                Monto = 0m,
                Activo = true,
                EsPredeterminado = true
            });

        _service = new CalculoService(
            _descuentoRepository.Object,
            _impuestoRepository.Object,
            _costoEnvioRepository.Object);
    }

    [Fact]
    public async Task CalcularVentaAsync_ImpuestoIncluido_No_Se_Suma_Dos_Veces()
    {
        ConfigurarImpuesto(1, "ISV incluido", "ISV15-I", 15m, incluido: true);

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 115m }
            }, null, null, null);

        Assert.Equal(115m, resultado.ImporteBruto);
        Assert.Equal(100m, resultado.Subtotal);
        Assert.Equal(15m, resultado.ImpuestoIncluido);
        Assert.Equal(0m, resultado.ImpuestoAdicional);
        Assert.Equal(115m, resultado.Total);
    }

    [Fact]
    public async Task CalcularVentaAsync_ImpuestoAdicional_Se_Suma_Al_Total()
    {
        ConfigurarImpuesto(2, "ISV adicional", "ISV15-A", 15m, incluido: false);

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m }
            }, null, null, null);

        Assert.Equal(100m, resultado.Subtotal);
        Assert.Equal(0m, resultado.ImpuestoIncluido);
        Assert.Equal(15m, resultado.ImpuestoAdicional);
        Assert.Equal(115m, resultado.Total);
    }

    [Fact]
    public async Task CalcularVentaAsync_Descuento_Se_Aplica_Antes_Del_Impuesto()
    {
        _descuentoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Descuento>
            {
                new()
                {
                    Id = 1,
                    Nombre = "Descuento general 10%",
                    Tipo = TipoDescuento.Porcentaje,
                    Valor = 10m,
                    Prioridad = 1,
                    Acumulable = false,
                    Activo = true
                }
            });

        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Venta))
            .ReturnsAsync(new List<Impuesto>
            {
                new()
                {
                    Id = 3,
                    Nombre = "ISV 15% después de descuento",
                    Codigo = "ISV15-D",
                    Tipo = TipoImpuesto.Porcentaje,
                    Tasa = 15m,
                    IncluidoEnPrecio = false,
                    SeCalculaAntesDescuento = false,
                    Acumulativo = true,
                    Activo = true
                }
            });

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m }
            }, null, null, null);

        Assert.Equal(10m, resultado.TotalDescuento);
        Assert.Equal(100m, resultado.Subtotal);
        Assert.Equal(13.50m, resultado.ImpuestoAdicional);
        Assert.Equal(103.50m, resultado.Total);
    }

    [Fact]
    public async Task CalcularVentaAsync_Total300_SeparaEnvio80_EImpuestoIncluido15()
    {
        _costoEnvioRepository
            .Setup(r => r.GetPredeterminadoVigenteAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new CostoEnvio
            {
                Id = 8,
                Nombre = "Envío estándar",
                Monto = 80m,
                Activo = true,
                EsPredeterminado = true
            });
        ConfigurarImpuesto(5, "ISV incluido", "ISV15-I", 15m, incluido: true);

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 300m }
            }, null, null, null);

        Assert.Equal(300m, resultado.ImporteBruto);
        Assert.Equal(220m, resultado.ImporteProductos);
        Assert.Equal(191.30m, resultado.Subtotal);
        Assert.Equal(28.70m, resultado.ImpuestoIncluido);
        Assert.Equal(80m, resultado.CostoEnvio);
        Assert.Equal(300m, resultado.Total);
    }

    [Fact]
    public async Task CalcularVentaAsync_Descuento20_ReduceTotal300_A280_SinDescontarEnvio()
    {
        _costoEnvioRepository
            .Setup(r => r.GetPredeterminadoVigenteAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new CostoEnvio
            {
                Id = 8,
                Nombre = "Envío estándar",
                Monto = 80m,
                Activo = true,
                EsPredeterminado = true
            });
        _descuentoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Descuento>
            {
                new()
                {
                    Id = 9,
                    Nombre = "Descuento L. 20",
                    Tipo = TipoDescuento.MontoFijo,
                    Valor = 20m,
                    Prioridad = 1,
                    Acumulable = false,
                    Activo = true
                }
            });
        ConfigurarImpuesto(5, "ISV incluido", "ISV15-I", 15m, incluido: true);

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 300m }
            }, null, null, null);

        Assert.Equal(20m, resultado.TotalDescuento);
        Assert.Equal(191.30m, resultado.Subtotal);
        Assert.Equal(28.70m, resultado.ImpuestoIncluido);
        Assert.Equal(80m, resultado.CostoEnvio);
        Assert.Equal(280m, resultado.Total);
    }

    [Fact]
    public async Task CalcularVentaAsync_CostoSeleccionado_PreservaCoberturaProfesional()
    {
        _costoEnvioRepository.Setup(r => r.GetByIdAsync(44)).ReturnsAsync(new CostoEnvio
        {
            Id = 44, Nombre = "Entrega Centro", Departamento = "Francisco Morazán", Ciudad = "Tegucigalpa",
            Zona = "Centro", Modalidad = "Entrega local", Monto = 80m, Activo = true
        });

        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput> { new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 300m } },
            null, null, null, costoEnvioId: 44);

        Assert.Equal("Francisco Morazán", resultado.CostoEnvioDepartamento);
        Assert.Equal("Tegucigalpa", resultado.CostoEnvioCiudad);
        Assert.Equal("Centro", resultado.CostoEnvioZona);
        Assert.Equal("Entrega local", resultado.CostoEnvioModalidad);
    }

    [Fact]
    public async Task CalcularVentaAsync_ExoneracionSinMotivo_EsRechazada()
    {
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CalcularVentaAsync(
                new List<DetalleCalculoInput>
                {
                    new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 300m }
                }, null, null, null, envioExonerado: true));

        Assert.Contains("motivo", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CalcularCompraAsync_ImpuestoIncluido_Extrae_Base_Sin_Modificar_Total_Proveedor()
    {
        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Compra))
            .ReturnsAsync(new List<Impuesto>
            {
                new()
                {
                    Id = 4,
                    Nombre = "Impuesto de compra incluido",
                    Codigo = "COMPRA15-I",
                    Tipo = TipoImpuesto.Porcentaje,
                    Tasa = 15m,
                    IncluidoEnPrecio = true,
                    Acumulativo = true,
                    Activo = true
                }
            });

        var resultado = await _service.CalcularCompraAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 1000m }
            }, proveedorId: 10);

        Assert.Equal(1000m, resultado.ImporteBruto);
        Assert.Equal(869.57m, resultado.Subtotal);
        Assert.Equal(130.43m, resultado.ImpuestoIncluido);
        Assert.Equal(1000m, resultado.Total);
    }

    [Fact]
    public async Task CalcularVentaAsync_RedondeaCadaLineaAntesDeSumar()
    {
        var resultado = await _service.CalcularVentaAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 0.335m },
                new() { ProductoId = 2, Cantidad = 1, PrecioUnitario = 0.335m }
            }, null, null, null);

        Assert.Equal(0.68m, resultado.ImporteBruto);
        Assert.Equal(0.68m, resultado.ImporteProductos);
        Assert.Equal(0.68m, resultado.Subtotal);
        Assert.Equal(0.68m, resultado.Total);
    }

    [Fact]
    public async Task CalcularCompraAsync_RedondeaCadaLineaAntesDeSumar()
    {
        var resultado = await _service.CalcularCompraAsync(
            new List<DetalleCalculoInput>
            {
                new() { ProductoId = 1, Cantidad = 1, PrecioUnitario = 10.005m },
                new() { ProductoId = 2, Cantidad = 1, PrecioUnitario = 10.005m }
            }, proveedorId: null);

        Assert.Equal(20.02m, resultado.ImporteBruto);
        Assert.Equal(20.02m, resultado.Subtotal);
        Assert.Equal(20.02m, resultado.Total);
    }

    private void ConfigurarImpuesto(int id, string nombre, string codigo, decimal tasa, bool incluido)
    {
        _impuestoRepository
            .Setup(r => r.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Venta))
            .ReturnsAsync(new List<Impuesto>
            {
                new()
                {
                    Id = id,
                    Nombre = nombre,
                    Codigo = codigo,
                    Tipo = TipoImpuesto.Porcentaje,
                    Tasa = tasa,
                    IncluidoEnPrecio = incluido,
                    Acumulativo = true,
                    Activo = true
                }
            });
    }
}