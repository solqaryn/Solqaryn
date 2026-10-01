using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Domain.Entities.Bancos;

public class MatchConciliacion : AuditableEntity
{
    public int MovimientoEstadoCuentaId { get; private set; }
    public MovimientoEstadoCuenta MovimientoEstadoCuenta { get; private set; } = null!;
    public int MovimientoFinancieroId { get; private set; }
    public decimal MontoAplicado { get; private set; }
    public TipoMatchConciliacion TipoMatch { get; private set; }

    protected MatchConciliacion() { }

    public MatchConciliacion(int movimientoFinancieroId, decimal montoAplicado, TipoMatchConciliacion tipoMatch)
    {
        if (movimientoFinancieroId <= 0) throw new ArgumentOutOfRangeException(nameof(movimientoFinancieroId), "El movimiento financiero debe ser válido.");
        if (montoAplicado <= 0) throw new ArgumentOutOfRangeException(nameof(montoAplicado), "El monto aplicado debe ser mayor a cero.");
        MovimientoFinancieroId = movimientoFinancieroId;
        MontoAplicado = montoAplicado;
        TipoMatch = tipoMatch;
    }
}
