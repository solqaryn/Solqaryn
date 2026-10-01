using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// N3.7.B — contrato mínimo de dominio para una nota de crédito de cliente ligada a una factura.
/// No define todavía lifecycle fiscal, numeración, aplicación contable/saldo, idempotencia, RBAC/HTTP,
/// devolución física, stock, Kardex ni caja. Esas decisiones permanecen fail-closed para N3.7.C/D+.
/// </summary>
public class NotaCreditoCliente : AuditableEntity
{
    private NotaCreditoCliente()
    {
    }

    public int FacturaId { get; private set; }
    public Factura Factura { get; private set; } = null!;
    public int VentaId { get; private set; }
    public string Moneda { get; private set; } = "HNL";
    public decimal MontoCredito { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public string? Observaciones { get; private set; }

    public static NotaCreditoCliente CrearDesdeFactura(
        Factura factura,
        decimal montoCredito,
        string motivo,
        string? observaciones = null)
    {
        ArgumentNullException.ThrowIfNull(factura);

        if (factura.Id <= 0)
            throw new InvalidOperationException("La factura de origen debe estar persistida.");
        if (factura.VentaId <= 0)
            throw new InvalidOperationException("La factura debe estar asociada a una venta válida.");
        if (factura.Estado is EstadoFactura.Borrador or EstadoFactura.Anulada or EstadoFactura.Cancelada)
            throw new InvalidOperationException("La factura no está en un estado elegible para nota de crédito.");
        if (string.IsNullOrWhiteSpace(factura.Moneda) || factura.Moneda.Trim().Length != 3)
            throw new InvalidOperationException("La moneda de la factura debe usar un código de tres caracteres.");
        if (montoCredito <= 0m)
            throw new ArgumentOutOfRangeException(nameof(montoCredito), "El monto acreditado debe ser mayor que cero.");
        if (factura.Total <= 0m || montoCredito > factura.Total)
            throw new InvalidOperationException("El monto acreditado no puede superar el total de la factura de origen.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de la nota de crédito es obligatorio.", nameof(motivo));

        var nota = new NotaCreditoCliente
        {
            FacturaId = factura.Id,
            Factura = factura,
            VentaId = factura.VentaId,
            Moneda = factura.Moneda.Trim().ToUpperInvariant(),
            MontoCredito = decimal.Round(montoCredito, 4, MidpointRounding.AwayFromZero),
            Motivo = motivo.Trim(),
            Observaciones = Normalizar(observaciones)
        };

        nota.ValidarDocumento();
        return nota;
    }

    public void ValidarDocumento()
    {
        if (FacturaId <= 0 || VentaId <= 0)
            throw new InvalidOperationException("La nota de crédito debe conservar una factura y venta de origen válidas.");
        if (string.IsNullOrWhiteSpace(Moneda) || Moneda.Length != 3)
            throw new InvalidOperationException("La moneda debe usar un código de tres caracteres.");
        if (MontoCredito <= 0m || Factura.Total <= 0m || MontoCredito > Factura.Total)
            throw new InvalidOperationException("El monto acreditado no es válido para la factura de origen.");
        if (string.IsNullOrWhiteSpace(Motivo))
            throw new InvalidOperationException("El motivo de la nota de crédito es obligatorio.");
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
