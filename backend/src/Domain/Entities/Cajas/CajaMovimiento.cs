using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums.Cajas;

namespace Solqaryn.Domain.Entities.Cajas;

public class CajaMovimiento : BaseEntity
{
    public int CajaSesionId { get; private set; }
    public int UsuarioId { get; private set; }
    public TipoMovimientoCaja Tipo { get; private set; }
    public decimal Monto { get; private set; }
    public string Referencia { get; private set; } = null!;
    public DateTime FechaOperacion { get; private set; }

    public decimal ImpactoSaldo => Tipo switch
    {
        TipoMovimientoCaja.Ingreso => Monto,
        TipoMovimientoCaja.Retiro => -Monto,
        TipoMovimientoCaja.DepositoBanco => -Monto,
        TipoMovimientoCaja.DiferenciaSobrante => Monto,
        TipoMovimientoCaja.DiferenciaFaltante => -Monto,
        _ => throw new InvalidOperationException("Tipo de movimiento de caja no soportado.")
    };

    protected CajaMovimiento() { }

    public CajaMovimiento(int cajaSesionId, int usuarioId, TipoMovimientoCaja tipo, decimal monto, string referencia)
    {
        if (cajaSesionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(cajaSesionId), "La sesión de caja debe estar persistida.");
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId));
        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo));
        if (monto <= 0)
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser mayor que cero.");
        if (string.IsNullOrWhiteSpace(referencia))
            throw new ArgumentException("La referencia es requerida.", nameof(referencia));

        CajaSesionId = cajaSesionId;
        UsuarioId = usuarioId;
        Tipo = tipo;
        Monto = monto;
        Referencia = referencia.Trim();
        FechaOperacion = DateTime.UtcNow;
    }
}
