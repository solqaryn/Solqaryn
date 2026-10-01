using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class RecepcionCompra : AuditableEntity
{
    public string NumeroRecepcion { get; set; } = string.Empty;
    public int OrdenCompraId { get; set; }
    public OrdenCompra OrdenCompra { get; set; } = null!;
    public EstadoRecepcionCompra Estado { get; private set; } = EstadoRecepcionCompra.Borrador;
    public string? Observaciones { get; set; }

    public string? IdempotencyKey { get; private set; }
    public string? IdempotencyFingerprint { get; private set; }

    public DateTime? FechaRecepcionUtc { get; private set; }
    public int? RecibidaPorUsuarioId { get; private set; }
    public string? RecibidaPorNombreSnapshot { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }
    public int? AnuladaPorUsuarioId { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public ICollection<RecepcionCompraDetalle> Detalles { get; set; } = new List<RecepcionCompraDetalle>();

    public bool EsEditable => Estado == EstadoRecepcionCompra.Borrador;
    public decimal CantidadRecibidaTotal => Detalles.Sum(x => x.CantidadRecibida);
    public decimal CantidadAceptadaTotal => Detalles.Sum(x => x.CantidadAceptada);
    public decimal CantidadDanadaTotal => Detalles.Sum(x => x.CantidadDanada);
    public decimal CantidadFaltanteTotal => Detalles.Sum(x => x.CantidadFaltante);
    public decimal CantidadSobranteTotal => Detalles.Sum(x => x.CantidadSobrante);

    public void EstablecerIdempotencia(string key, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 128)
            throw new ArgumentException("La clave de idempotencia es obligatoria y no puede superar 128 caracteres.", nameof(key));

        var fingerprintNormalizado = fingerprint?.Trim();
        if (string.IsNullOrWhiteSpace(fingerprintNormalizado) ||
            fingerprintNormalizado.Length != 64 ||
            !fingerprintNormalizado.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("El fingerprint de idempotencia debe ser SHA-256 hexadecimal.", nameof(fingerprint));
        }

        if (IdempotencyKey is not null && !string.Equals(IdempotencyKey, key.Trim(), StringComparison.Ordinal))
            throw new InvalidOperationException("La clave de idempotencia de una recepción no puede sustituirse.");
        if (IdempotencyFingerprint is not null && !string.Equals(IdempotencyFingerprint, fingerprintNormalizado, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El fingerprint de idempotencia de una recepción no puede sustituirse.");

        IdempotencyKey = key.Trim();
        IdempotencyFingerprint = fingerprintNormalizado.ToLowerInvariant();
    }

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una recepción en borrador puede modificarse.");
    }

    public void Confirmar(int usuarioId, string? usuarioNombre, DateTime fechaUtc)
    {
        if (Estado != EstadoRecepcionCompra.Borrador)
            throw new InvalidOperationException("Solo una recepción en borrador puede confirmarse.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();

        if (!Detalles.Any(x => x.CantidadRecibida > 0))
            throw new InvalidOperationException("La recepción debe registrar al menos una cantidad físicamente recibida.");

        Estado = EstadoRecepcionCompra.Recibida;
        FechaRecepcionUtc = fechaUtc;
        RecibidaPorUsuarioId = usuarioId;
        RecibidaPorNombreSnapshot = Normalizar(usuarioNombre);
    }

    public void Anular(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoRecepcionCompra.Recibida)
            throw new InvalidOperationException("Solo una recepción materializada puede anularse.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        ValidarUsuario(usuarioId);

        Estado = EstadoRecepcionCompra.Anulada;
        FechaAnulacionUtc = fechaUtc;
        AnuladaPorUsuarioId = usuarioId;
        MotivoAnulacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(NumeroRecepcion))
            throw new InvalidOperationException("El número de recepción es obligatorio.");
        if (OrdenCompraId <= 0)
            throw new InvalidOperationException("La orden de compra es obligatoria.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La recepción debe contener al menos un detalle.");
        if ((IdempotencyKey is null) != (IdempotencyFingerprint is null))
            throw new InvalidOperationException("La idempotencia de la recepción debe persistir clave y fingerprint de forma atómica.");

        foreach (var detalle in Detalles)
            detalle.Validar();

        var duplicadosFisicos = Detalles
            .GroupBy(x => new { x.OrdenCompraDetalleId, x.AlmacenId, x.UbicacionAlmacenId })
            .Any(grupo => grupo.Count() > 1);
        if (duplicadosFisicos)
            throw new InvalidOperationException("Una línea de orden no puede duplicar la misma clave física de destino dentro de la recepción.");
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}