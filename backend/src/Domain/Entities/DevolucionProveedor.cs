using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class DevolucionProveedor : AuditableEntity
{
    public string NumeroDevolucion { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public int OrdenCompraId { get; set; }
    public int RecepcionCompraId { get; set; }
    public int FacturaProveedorId { get; set; }
    public string ProveedorNombreSnapshot { get; set; } = string.Empty;
    public string Moneda { get; set; } = "HNL";
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    public string? IdempotencyKey { get; private set; }
    public string? IdempotencyFingerprint { get; private set; }

    public EstadoDevolucionProveedor Estado { get; private set; } = EstadoDevolucionProveedor.Borrador;
    public DateTime? FechaConfirmacionUtc { get; private set; }
    public int? ConfirmadaPorUsuarioId { get; private set; }
    public string? ConfirmadaPorNombreSnapshot { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }
    public int? AnuladaPorUsuarioId { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public ICollection<DevolucionProveedorDetalle> Detalles { get; set; } = new List<DevolucionProveedorDetalle>();

    public bool EsEditable => Estado == EstadoDevolucionProveedor.Borrador;
    public decimal SubtotalCredito => decimal.Round(Detalles.Sum(x => x.SubtotalCredito), 4, MidpointRounding.AwayFromZero);
    public decimal ImpuestoCredito => decimal.Round(Detalles.Sum(x => x.ImpuestoCredito), 4, MidpointRounding.AwayFromZero);
    public decimal TotalCredito => SubtotalCredito + ImpuestoCredito;

    public void EstablecerIdempotencia(string key, string fingerprint)
    {
        var keyNormalizada = key?.Trim();
        var fingerprintNormalizado = fingerprint?.Trim();

        if (string.IsNullOrWhiteSpace(keyNormalizada) || keyNormalizada.Length > 128)
            throw new ArgumentException("La clave de idempotencia es obligatoria y no puede superar 128 caracteres.", nameof(key));
        if (string.IsNullOrWhiteSpace(fingerprintNormalizado) || fingerprintNormalizado.Length != 64 || !fingerprintNormalizado.All(Uri.IsHexDigit))
            throw new ArgumentException("El fingerprint de idempotencia debe ser SHA-256 hexadecimal.", nameof(fingerprint));
        if (IdempotencyKey is not null && !string.Equals(IdempotencyKey, keyNormalizada, StringComparison.Ordinal))
            throw new InvalidOperationException("La clave de idempotencia de una devolución no puede sustituirse.");
        if (IdempotencyFingerprint is not null && !string.Equals(IdempotencyFingerprint, fingerprintNormalizado, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El fingerprint de idempotencia de una devolución no puede sustituirse.");

        IdempotencyKey = keyNormalizada;
        IdempotencyFingerprint = fingerprintNormalizado.ToLowerInvariant();
    }

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una devolución a proveedor en borrador puede modificarse.");
    }

    public void Confirmar(int usuarioId, string? usuarioNombre, DateTime fechaUtc)
    {
        if (Estado != EstadoDevolucionProveedor.Borrador)
            throw new InvalidOperationException("Solo una devolución a proveedor en borrador puede confirmarse.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();

        Estado = EstadoDevolucionProveedor.Confirmada;
        FechaConfirmacionUtc = fechaUtc;
        ConfirmadaPorUsuarioId = usuarioId;
        ConfirmadaPorNombreSnapshot = Normalizar(usuarioNombre);
    }

    public void Anular(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoDevolucionProveedor.Confirmada)
            throw new InvalidOperationException("Solo una devolución a proveedor confirmada puede anularse.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        ValidarUsuario(usuarioId);

        Estado = EstadoDevolucionProveedor.Anulada;
        FechaAnulacionUtc = fechaUtc;
        AnuladaPorUsuarioId = usuarioId;
        MotivoAnulacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(NumeroDevolucion))
            throw new InvalidOperationException("El número de devolución es obligatorio.");
        if (ProveedorId <= 0)
            throw new InvalidOperationException("El proveedor es obligatorio.");
        if (OrdenCompraId <= 0)
            throw new InvalidOperationException("La orden de compra es obligatoria.");
        if (RecepcionCompraId <= 0)
            throw new InvalidOperationException("La recepción de compra es obligatoria.");
        if (FacturaProveedorId <= 0)
            throw new InvalidOperationException("La factura de proveedor es obligatoria para materializar el crédito de la devolución.");
        if (string.IsNullOrWhiteSpace(ProveedorNombreSnapshot))
            throw new InvalidOperationException("El snapshot del proveedor es obligatorio.");
        if (string.IsNullOrWhiteSpace(Moneda) || Moneda.Trim().Length != 3)
            throw new InvalidOperationException("La moneda debe usar un código ISO de tres caracteres.");
        if (string.IsNullOrWhiteSpace(Motivo))
            throw new InvalidOperationException("El motivo de devolución es obligatorio.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La devolución debe contener al menos un detalle.");
        if ((IdempotencyKey is null) != (IdempotencyFingerprint is null))
            throw new InvalidOperationException("La idempotencia debe persistir clave y fingerprint de forma atómica.");

        foreach (var detalle in Detalles)
            detalle.Validar();

        if (Detalles.GroupBy(x => x.RecepcionCompraDetalleId).Any(x => x.Count() > 1))
            throw new InvalidOperationException("Una línea de recepción no puede repetirse dentro de la misma devolución.");
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
