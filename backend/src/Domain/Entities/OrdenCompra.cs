using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class OrdenCompra : AuditableEntity
{
    public string NumeroOrden { get; set; } = string.Empty;
    public EstadoOrdenCompra Estado { get; private set; } = EstadoOrdenCompra.Borrador;

    public int? SolicitudCompraId { get; set; }
    public SolicitudCompra? SolicitudCompra { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public string ProveedorNombreSnapshot { get; set; } = string.Empty;
    public string? ProveedorDocumentoSnapshot { get; set; }

    public string Moneda { get; set; } = "HNL";
    public string? CondicionesCompra { get; set; }
    public DateTime? FechaEsperadaUtc { get; set; }
    public string? Observaciones { get; set; }

    // N2.2.D — la idempotencia de creación es un atributo durable del documento.
    // El fingerprint nunca se expone por API; solo permite distinguir un replay
    // legítimo del reuso de una misma clave con un payload diferente.
    public string? IdempotencyKey { get; private set; }
    public string? IdempotencyFingerprint { get; private set; }

    public DateTime? FechaEnvioAprobacionUtc { get; private set; }
    public int? EnviadaAprobacionPorUsuarioId { get; private set; }
    public DateTime? FechaAprobacionUtc { get; private set; }
    public int? AprobadaPorUsuarioId { get; private set; }
    public string? AprobadaPorNombreSnapshot { get; private set; }
    public DateTime? FechaCancelacionUtc { get; private set; }
    public int? CanceladaPorUsuarioId { get; private set; }
    public string? MotivoCancelacion { get; private set; }

    public ICollection<OrdenCompraDetalle> Detalles { get; set; } = new List<OrdenCompraDetalle>();

    public bool EsEditable => Estado == EstadoOrdenCompra.Borrador;
    public decimal Subtotal => Detalles.Sum(x => x.Subtotal);
    public decimal Descuento => Detalles.Sum(x => x.Descuento);
    public decimal Impuesto => Detalles.Sum(x => x.Impuesto);
    public decimal Total => Detalles.Sum(x => x.Total);

    public void EstablecerIdempotencia(string key, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 128)
            throw new ArgumentException("La clave de idempotencia es obligatoria y no puede superar 128 caracteres.", nameof(key));
        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Trim().Length != 64)
            throw new ArgumentException("El fingerprint de idempotencia debe ser SHA-256 hexadecimal.", nameof(fingerprint));
        if (IdempotencyKey is not null && !string.Equals(IdempotencyKey, key.Trim(), StringComparison.Ordinal))
            throw new InvalidOperationException("La clave de idempotencia de una orden no puede sustituirse.");
        if (IdempotencyFingerprint is not null && !string.Equals(IdempotencyFingerprint, fingerprint.Trim(), StringComparison.Ordinal))
            throw new InvalidOperationException("El fingerprint de idempotencia de una orden no puede sustituirse.");

        IdempotencyKey = key.Trim();
        IdempotencyFingerprint = fingerprint.Trim().ToLowerInvariant();
    }

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una orden de compra en borrador puede modificarse.");
    }

    public void EnviarAprobacion(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoOrdenCompra.Borrador)
            throw new InvalidOperationException("Solo una orden en borrador puede enviarse a aprobación.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();

        Estado = EstadoOrdenCompra.PendienteAprobacion;
        FechaEnvioAprobacionUtc = fechaUtc;
        EnviadaAprobacionPorUsuarioId = usuarioId;
    }

    public void Aprobar(int usuarioId, string? usuarioNombre, DateTime fechaUtc)
    {
        if (Estado != EstadoOrdenCompra.PendienteAprobacion)
            throw new InvalidOperationException("Solo una orden pendiente de aprobación puede aprobarse.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();

        Estado = EstadoOrdenCompra.Aprobada;
        FechaAprobacionUtc = fechaUtc;
        AprobadaPorUsuarioId = usuarioId;
        AprobadaPorNombreSnapshot = Normalizar(usuarioNombre);
    }

    public void Cancelar(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado == EstadoOrdenCompra.Cancelada)
            throw new InvalidOperationException("La orden de compra ya está cancelada.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(motivo));

        ValidarUsuario(usuarioId);

        Estado = EstadoOrdenCompra.Cancelada;
        FechaCancelacionUtc = fechaUtc;
        CanceladaPorUsuarioId = usuarioId;
        MotivoCancelacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(NumeroOrden))
            throw new InvalidOperationException("El número de orden es obligatorio.");
        if (ProveedorId <= 0)
            throw new InvalidOperationException("El proveedor es obligatorio.");
        if (string.IsNullOrWhiteSpace(ProveedorNombreSnapshot))
            throw new InvalidOperationException("El snapshot del proveedor es obligatorio.");
        if (string.IsNullOrWhiteSpace(Moneda) || Moneda.Trim().Length != 3)
            throw new InvalidOperationException("La moneda debe usar un código ISO de tres caracteres.");
        if (SolicitudCompraId is <= 0)
            throw new InvalidOperationException("La solicitud de compra debe ser válida cuando se especifica.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La orden de compra debe contener al menos un detalle.");
        if ((IdempotencyKey is null) != (IdempotencyFingerprint is null))
            throw new InvalidOperationException("La idempotencia de la orden debe persistir clave y fingerprint de forma atómica.");

        foreach (var detalle in Detalles)
            detalle.Validar();
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
