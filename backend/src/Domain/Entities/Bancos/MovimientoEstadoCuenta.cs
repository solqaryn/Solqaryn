using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Domain.Entities.Bancos;

public class MovimientoEstadoCuenta : AuditableEntity
{
    public int ConciliacionBancariaId { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public DateTime FechaMovimiento { get; private set; }
    public string Concepto { get; private set; } = null!;
    public string Referencia { get; private set; } = string.Empty;
    public TipoMovimientoEstadoCuenta Tipo { get; private set; }
    public decimal Monto { get; private set; }
    public EstadoMovimientoEstadoCuenta Estado { get; private set; }

    private readonly List<MatchConciliacion> _matches = new();
    public IReadOnlyCollection<MatchConciliacion> Matches => _matches.AsReadOnly();

    protected MovimientoEstadoCuenta() { }

    public MovimientoEstadoCuenta(string idempotencyKey, DateTime fechaMovimiento, string concepto, string referencia, TipoMovimientoEstadoCuenta tipo, decimal monto)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("IdempotencyKey es requerida.", nameof(idempotencyKey));
        if (string.IsNullOrWhiteSpace(concepto)) throw new ArgumentException("El concepto es requerido.", nameof(concepto));
        if (monto <= 0) throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser mayor a cero.");
        IdempotencyKey = idempotencyKey;
        FechaMovimiento = fechaMovimiento;
        Concepto = concepto.Trim();
        Referencia = referencia?.Trim() ?? string.Empty;
        Tipo = tipo;
        Monto = monto;
        Estado = EstadoMovimientoEstadoCuenta.Pendiente;
    }

    public decimal MontoConciliado => _matches.Sum(m => m.MontoAplicado);
    public decimal MontoPendiente => Monto - MontoConciliado;

    public void AgregarMatch(int movimientoFinancieroId, decimal montoAplicado, TipoMatchConciliacion tipoMatch)
    {
        if (Estado == EstadoMovimientoEstadoCuenta.Ignorado) throw new InvalidOperationException("No se puede hacer match a un movimiento ignorado.");
        if (Estado == EstadoMovimientoEstadoCuenta.Conciliado) throw new InvalidOperationException("El movimiento ya está conciliado.");
        if (montoAplicado <= 0) throw new ArgumentOutOfRangeException(nameof(montoAplicado), "El monto aplicado debe ser mayor a cero.");
        if (MontoPendiente < montoAplicado) throw new InvalidOperationException("El monto aplicado supera el saldo pendiente del movimiento.");
        if (_matches.Any(m => m.MovimientoFinancieroId == movimientoFinancieroId)) throw new InvalidOperationException("El movimiento financiero ya fue aplicado a este movimiento bancario.");
        _matches.Add(new MatchConciliacion(movimientoFinancieroId, montoAplicado, tipoMatch));
        EvaluarEstado();
        FechaActualizacion = DateTime.UtcNow;
    }

    public void RemoverMatch(int movimientoFinancieroId)
    {
        var match = _matches.FirstOrDefault(m => m.MovimientoFinancieroId == movimientoFinancieroId);
        if (match == null) throw new InvalidOperationException("El match no existe.");
        _matches.Remove(match);
        EvaluarEstado();
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Ignorar()
    {
        if (_matches.Any()) throw new InvalidOperationException("No se puede ignorar un movimiento con matches.");
        Estado = EstadoMovimientoEstadoCuenta.Ignorado;
        FechaActualizacion = DateTime.UtcNow;
    }

    private void EvaluarEstado()
    {
        if (Estado == EstadoMovimientoEstadoCuenta.Ignorado) return;
        if (MontoConciliado == 0) Estado = EstadoMovimientoEstadoCuenta.Pendiente;
        else if (MontoConciliado < Monto) Estado = EstadoMovimientoEstadoCuenta.Parcial;
        else Estado = EstadoMovimientoEstadoCuenta.Conciliado;
    }
}
