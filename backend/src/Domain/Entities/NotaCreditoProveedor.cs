using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Documento financiero emitido por un proveedor para acreditar total o parcialmente una factura.
/// El documento no materializa por sí mismo movimientos físicos de inventario ni aplica saldos de CxP;
/// esas responsabilidades pertenecen a los puntos funcionales que las gobiernan.
/// </summary>
public class NotaCreditoProveedor : AuditableEntity
{
    public string NumeroNotaCredito { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public int FacturaProveedorId { get; set; }
    public int? DevolucionProveedorId { get; set; }

    public string ProveedorNombreSnapshot { get; set; } = string.Empty;
    public string Moneda { get; set; } = "HNL";
    public DateTime FechaEmisionUtc { get; set; }
    public string? ReferenciaFiscal { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }

    public decimal SubtotalCredito { get; set; }
    public decimal ImpuestoCredito { get; set; }
    public decimal TotalCredito => Redondear(SubtotalCredito + ImpuestoCredito);

    public EstadoNotaCreditoProveedor Estado { get; private set; } = EstadoNotaCreditoProveedor.Borrador;
    public DateTime? FechaRegistroUtc { get; private set; }
    public int? RegistradaPorUsuarioId { get; private set; }
    public string? RegistradaPorNombreSnapshot { get; private set; }
    public DateTime? FechaAnulacionUtc { get; private set; }
    public int? AnuladaPorUsuarioId { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    public bool EsEditable => Estado == EstadoNotaCreditoProveedor.Borrador;

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una nota de crédito de proveedor en borrador puede modificarse.");
    }

    public void Registrar(int usuarioId, string? usuarioNombre, DateTime fechaUtc)
    {
        if (Estado != EstadoNotaCreditoProveedor.Borrador)
            throw new InvalidOperationException("Solo una nota de crédito de proveedor en borrador puede registrarse.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();

        Estado = EstadoNotaCreditoProveedor.Registrada;
        FechaRegistroUtc = fechaUtc;
        RegistradaPorUsuarioId = usuarioId;
        RegistradaPorNombreSnapshot = Normalizar(usuarioNombre);
    }

    public void Anular(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoNotaCreditoProveedor.Registrada)
            throw new InvalidOperationException("Solo una nota de crédito de proveedor registrada puede anularse.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        ValidarUsuario(usuarioId);

        Estado = EstadoNotaCreditoProveedor.Anulada;
        FechaAnulacionUtc = fechaUtc;
        AnuladaPorUsuarioId = usuarioId;
        MotivoAnulacion = motivo.Trim();
    }

    public void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(NumeroNotaCredito))
            throw new InvalidOperationException("El número de la nota de crédito del proveedor es obligatorio.");
        if (ProveedorId <= 0)
            throw new InvalidOperationException("El proveedor es obligatorio.");
        if (FacturaProveedorId <= 0)
            throw new InvalidOperationException("La factura de proveedor acreditada es obligatoria.");
        if (DevolucionProveedorId is <= 0)
            throw new InvalidOperationException("La devolución de proveedor, cuando se informa, debe ser válida.");
        if (string.IsNullOrWhiteSpace(ProveedorNombreSnapshot))
            throw new InvalidOperationException("El snapshot del proveedor es obligatorio.");
        if (string.IsNullOrWhiteSpace(Moneda) || Moneda.Trim().Length != 3)
            throw new InvalidOperationException("La moneda debe usar un código ISO de tres caracteres.");
        if (FechaEmisionUtc == default)
            throw new InvalidOperationException("La fecha de emisión es obligatoria.");
        if (string.IsNullOrWhiteSpace(Motivo))
            throw new InvalidOperationException("El motivo de la nota de crédito es obligatorio.");
        if (SubtotalCredito < 0m)
            throw new InvalidOperationException("El subtotal acreditado no puede ser negativo.");
        if (ImpuestoCredito < 0m)
            throw new InvalidOperationException("El impuesto acreditado no puede ser negativo.");
        if (TotalCredito <= 0m)
            throw new InvalidOperationException("El total acreditado debe ser mayor que cero.");
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static decimal Redondear(decimal valor) => decimal.Round(valor, 4, MidpointRounding.AwayFromZero);
    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
