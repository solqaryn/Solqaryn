using Solqaryn.Domain.Common;
using Solqaryn.Domain.Fiscal;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Registro durable y provider-neutral de una emisión fiscal/electrónica.
/// Conserva únicamente identificadores opacos y hashes; las reglas legales y
/// credenciales específicas de cada jurisdicción/proveedor viven fuera del core.
/// </summary>
public sealed class DocumentoFiscalEmision : AuditableEntity
{
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public int FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public string Jurisdiccion { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hexadecimal de la clave de idempotencia; la clave original no se persiste.
    /// </summary>
    public string ClaveIdempotenciaHash { get; set; } = string.Empty;

    /// <summary>
    /// Huella del snapshot inmutable enviado al adaptador fiscal.
    /// </summary>
    public string HashSnapshot { get; set; } = string.Empty;

    public EstadoResultadoFiscal Estado { get; set; } = EstadoResultadoFiscal.Pendiente;
    public string? ReferenciaExterna { get; set; }
    public string? CodigoProveedor { get; set; }
    public bool EsTransitorio { get; set; }
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime UltimaActualizacionUtc { get; set; } = DateTime.UtcNow;
}
