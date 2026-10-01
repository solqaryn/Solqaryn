namespace Solqaryn.Application.DTOs;

public class FacturaDetalleDto
{
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoMarca { get; set; } = string.Empty;
    public string ProductoModelo { get; set; } = string.Empty;
    public string? VarianteColor { get; set; }
    public string? VarianteTalla { get; set; }
    public string? VarianteSku { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalLinea { get; set; }
    public string? Observaciones { get; set; }
}

public class FacturaPagoDto
{
    public int Id { get; set; }
    public DateTime FechaPago { get; set; }
    /// <summary>Importe aplicado contablemente a la factura.</summary>
    public decimal Monto { get; set; }
    /// <summary>Importe efectivamente recibido antes de devolver cambio.</summary>
    public decimal MontoRecibido { get; set; }
    /// <summary>Cambio devuelto al cliente.</summary>
    public decimal Cambio { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public int? BancoId { get; set; }
    public string? BancoCodigo { get; set; }
    public string? BancoNombre { get; set; }
    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }
    public bool Anulado { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string? MotivoAnulacion { get; set; }
}

public class RegistrarFacturaPagoDto
{
    /// <summary>Importe entregado/recibido. Si el método permite cambio puede superar el saldo pendiente.</summary>
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public int? BancoId { get; set; }
    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaPago { get; set; }
}

public class AnularFacturaPagoDto
{
    public string Motivo { get; set; } = string.Empty;
}

public class CambiarEstadoFacturaDto
{
    public string Estado { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}

public class FacturaDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public string NumeroVentaOrigen { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public string? CodigoInterno { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Moneda { get; set; } = "HNL";
    public string? CondicionPago { get; set; }
    public string? Referencia { get; set; }

    public string EmpresaNombre { get; set; } = string.Empty;
    public string? EmpresaRTN { get; set; }
    public string? EmpresaTelefono { get; set; }
    public string? EmpresaCorreo { get; set; }
    public string? EmpresaDireccion { get; set; }
    public string? EmpresaEslogan { get; set; }
    public string? EmpresaTextoFactura { get; set; }
    public string? EmpresaTextoLegal { get; set; }
    public string? EmpresaCopyright { get; set; }
    public string? EmpresaLogoUrl { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;
    public string? ClienteTelefono { get; set; }
    public string? ClienteIdentidadORTN { get; set; }
    public string? ClienteCorreo { get; set; }
    public string? ClienteDireccion { get; set; }

    public string VendedorNombreUsuario { get; set; } = string.Empty;
    public string? GeneradaPorNombreUsuario { get; set; }

    public decimal ImporteBruto { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal ImpuestoIncluido { get; set; }
    public decimal ImpuestoAdicional { get; set; }
    public decimal CostoEnvio { get; set; }
    public int? CostoEnvioId { get; set; }
    public string? CostoEnvioNombre { get; set; }
    public string? CostoEnvioDepartamento { get; set; }
    public string? CostoEnvioCiudad { get; set; }
    public string? CostoEnvioZona { get; set; }
    public string? CostoEnvioModalidad { get; set; }
    public bool EnvioExonerado { get; set; }
    public string? MotivoExoneracionEnvio { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;

    public string? Observaciones { get; set; }
    public List<FacturaDetalleDto> Detalles { get; set; } = new();
    public List<FacturaPagoDto> Pagos { get; set; } = new();
    public List<DescuentoAplicadoDto> DescuentosAplicados { get; set; } = new();
    public List<ImpuestoAplicadoDto> ImpuestosAplicados { get; set; } = new();

    public DateTime? FechaAnulacion { get; set; }
    public string? AnuladaPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }
}
