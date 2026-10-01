using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class FacturaPago : AuditableEntity
{
    public int FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    /// <summary>Importe efectivamente aplicado a la deuda de la factura.</summary>
    public decimal Monto { get; set; }
    /// <summary>Importe entregado/recibido antes de calcular cambio.</summary>
    public decimal MontoRecibido { get; set; }
    /// <summary>Cambio devuelto al cliente; nunca forma parte del total pagado.</summary>
    public decimal Cambio { get; set; }

    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;
    public int? MetodoPagoId { get; set; }
    public Solqaryn.Domain.Entities.Catalogos.MetodoPago? MetodoPagoCatalogo { get; set; }
    /// <summary>Código inmutable del método de pago al momento de registrar el pago.</summary>
    public string? MetodoPagoCodigoSnapshot { get; set; }
    /// <summary>Nombre inmutable del método de pago al momento de registrar el pago.</summary>
    public string? MetodoPagoNombreSnapshot { get; set; }

    public int? BancoId { get; set; }
    public Solqaryn.Domain.Entities.Catalogos.Banco? Banco { get; set; }
    public string? BancoCodigoSnapshot { get; set; }
    public string? BancoNombreSnapshot { get; set; }

    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }

    public bool Anulado { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? AnuladoPorUsuarioId { get; set; }
    public string? AnuladoPorNombreUsuario { get; set; }
    public string? MotivoAnulacion { get; set; }
}
