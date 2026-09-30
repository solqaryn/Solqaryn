using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class Factura
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public string? CodigoInterno { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public DateTime? FechaVencimiento { get; set; }
    public EstadoFactura Estado { get; set; } = EstadoFactura.Emitida;
    public string Moneda { get; set; } = "HNL";
    public string? CondicionPago { get; set; }
    public string? Referencia { get; set; }
    public string EmpresaNombre { get; set; } = string.Empty;
    public string? EmpresaRTN { get; set; }
    public string? EmpresaTelefono { get; set; }
    public string? EmpresaCorreo { get; set; }
    public string? EmpresaDireccion { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string? ClienteTelefono { get; set; }
    public string? ClienteIdentidadORTN { get; set; }
    public string? ClienteCorreo { get; set; }
    public string? ClienteDireccion { get; set; }
    public int VendedorUsuarioId { get; set; }
    public string VendedorNombreUsuario { get; set; } = string.Empty;
    public int? GeneradaPorUsuarioId { get; set; }
    public string? GeneradaPorNombreUsuario { get; set; }
    public decimal ImporteBruto { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal CostoEnvio { get; set; }
    public int? CostoEnvioId { get; set; }
    public string? CostoEnvioNombreSnapshot { get; set; }
    public string? CostoEnvioDepartamentoSnapshot { get; set; }
    public string? CostoEnvioCiudadSnapshot { get; set; }
    public string? CostoEnvioZonaSnapshot { get; set; }
    public string? CostoEnvioModalidadSnapshot { get; set; }
    public decimal? CostoEnvioMontoSnapshot { get; set; }
    public bool EnvioExonerado { get; set; }
    public string? MotivoExoneracionEnvio { get; set; }
    public decimal Total { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string? MetodoPagoCodigoSnapshot { get; set; }
    public string? MetodoPagoNombreSnapshot { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? AnuladaPorUsuarioId { get; set; }
    public string? AnuladaPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }
    public ICollection<FacturaDetalle> Detalles { get; set; } = new List<FacturaDetalle>();
    public ICollection<FacturaPago> Pagos { get; set; } = new List<FacturaPago>();
}
