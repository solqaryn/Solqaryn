namespace Solqaryn.Application.DTOs;

public class DetalleCalculoInput
{
    public int ProductoId { get; set; }
    public int? CategoriaId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class DescuentoAplicadoDto
{
    public int DescuentoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public decimal Monto { get; set; }
}

public class ImpuestoAplicadoDto
{
    public int ImpuestoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public decimal Tasa { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Monto { get; set; }
    public bool IncluidoEnPrecio { get; set; }
}

public class ResultadoCalculoDto
{
    public decimal ImporteBruto { get; set; }
    public decimal ImporteProductos { get; set; }
    public decimal Subtotal { get; set; }
    public decimal SubtotalNeto
    {
        get => Subtotal;
        set => Subtotal = value;
    }

    public List<DescuentoAplicadoDto> DescuentosAplicados { get; set; } = new();
    public decimal TotalDescuento { get; set; }
    public List<ImpuestoAplicadoDto> ImpuestosAplicados { get; set; } = new();
    public decimal TotalImpuesto { get; set; }
    public decimal ImpuestoIncluido { get; set; }
    public decimal ImpuestoAdicional { get; set; }

    public int? CostoEnvioId { get; set; }
    public string? CostoEnvioNombre { get; set; }
    public string? CostoEnvioDepartamento { get; set; }
    public string? CostoEnvioCiudad { get; set; }
    public string? CostoEnvioZona { get; set; }
    public string? CostoEnvioModalidad { get; set; }
    public decimal CostoEnvio { get; set; }
    public bool EnvioExonerado { get; set; }
    public string? MotivoExoneracionEnvio { get; set; }

    public decimal Total { get; set; }
}

public class CalcularVentaRequest
{
    public int? ClienteId { get; set; }
    public string? CodigoPromocional { get; set; }
    public int? CostoEnvioId { get; set; }
    public bool EnvioExonerado { get; set; }
    public string? MotivoExoneracionEnvio { get; set; }
    public List<VentaDetalleInputDto> Detalles { get; set; } = new();
}

public class CalcularCompraRequest
{
    public int? ProveedorId { get; set; }
    public List<VentaDetalleInputDto> Detalles { get; set; } = new();
}
