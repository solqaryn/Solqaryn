using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Intento transaccional de pago online asociado a una factura y a un tenant.
/// El contrato es provider-agnostic y no almacena credenciales del proveedor.
/// </summary>
public class PagoOnline : AuditableEntity
{
    public int EmpresaId { get; set; }
    public int FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public string Proveedor { get; set; } = string.Empty;
    public string? ReferenciaProveedor { get; set; }
    public string? ProviderEventId { get; set; }

    /// <summary>
    /// SHA-256 hexadecimal de Idempotency-Key. La clave enviada por el cliente no
    /// se persiste en claro y la unicidad tenant+provider hace durable el replay.
    /// </summary>
    public string? ClaveIdempotenciaHash { get; set; }

    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "HNL";
    public EstadoPagoOnline Estado { get; set; } = EstadoPagoOnline.Pendiente;

    public string? UrlPago { get; set; }
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmadoUtc { get; set; }
    public string? UltimoError { get; set; }
}
