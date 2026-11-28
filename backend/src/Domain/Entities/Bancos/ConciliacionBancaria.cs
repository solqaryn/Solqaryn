using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Domain.Entities.Bancos;

public class ConciliacionBancaria : AuditableEntity
{
    public int CuentaBancariaId { get; private set; }
    public CuentaBancaria CuentaBancaria { get; private set; } = null!;
    public DateTime FechaInicio { get; private set; }
    public DateTime FechaFin { get; private set; }
    public decimal SaldoInicialBanco { get; private set; }
    public decimal SaldoFinalBanco { get; private set; }
    public EstadoConciliacionBancaria Estado { get; private set; }
    public string? Observaciones { get; private set; }

    private readonly List<MovimientoEstadoCuenta> _movimientos = new();
    public IReadOnlyCollection<MovimientoEstadoCuenta> Movimientos => _movimientos.AsReadOnly();

    protected ConciliacionBancaria() { }

    public ConciliacionBancaria(int cuentaBancariaId, DateTime fechaInicio, DateTime fechaFin, decimal saldoInicialBanco, decimal saldoFinalBanco, string? observaciones = null)
    {
        if (cuentaBancariaId <= 0) throw new ArgumentOutOfRangeException(nameof(cuentaBancariaId), "La cuenta bancaria es requerida.");
        if (fechaFin < fechaInicio) throw new ArgumentException("La fecha final no puede ser menor a la fecha inicial.");
        if (saldoInicialBanco < 0) throw new ArgumentOutOfRangeException(nameof(saldoInicialBanco), "El saldo inicial no puede ser negativo.");
        if (saldoFinalBanco < 0) throw new ArgumentOutOfRangeException(nameof(saldoFinalBanco), "El saldo final no puede ser negativo.");

        CuentaBancariaId = cuentaBancariaId;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        SaldoInicialBanco = saldoInicialBanco;
        SaldoFinalBanco = saldoFinalBanco;
        Observaciones = observaciones?.Trim();
        Estado = EstadoConciliacionBancaria.Borrador;
    }

    public decimal SaldoConciliado
    {
        get
        {
            var creditosConciliados = _movimientos.Where(m => m.Tipo == TipoMovimientoEstadoCuenta.Credito && m.Estado != EstadoMovimientoEstadoCuenta.Ignorado).Sum(m => m.MontoConciliado);
            var debitosConciliados = _movimientos.Where(m => m.Tipo == TipoMovimientoEstadoCuenta.Debito && m.Estado != EstadoMovimientoEstadoCuenta.Ignorado).Sum(m => m.MontoConciliado);
            return SaldoInicialBanco + creditosConciliados - debitosConciliados;
        }
    }

    public decimal Diferencia => SaldoFinalBanco - SaldoConciliado;

    public void AgregarMovimiento(MovimientoEstadoCuenta movimiento)
    {
        if (Estado != EstadoConciliacionBancaria.Borrador && Estado != EstadoConciliacionBancaria.EnProceso) throw new InvalidOperationException("Solo se pueden agregar movimientos en estado Borrador o EnProceso.");
        if (movimiento == null) throw new ArgumentNullException(nameof(movimiento));
        if (_movimientos.Any(m => m.IdempotencyKey == movimiento.IdempotencyKey)) throw new InvalidOperationException("Ya existe un movimiento con la misma llave de idempotencia.");
        _movimientos.Add(movimiento);
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarComoEnProceso()
    {
        if (Estado != EstadoConciliacionBancaria.Borrador) throw new InvalidOperationException("La conciliación debe estar en Borrador para pasar a EnProceso.");
        Estado = EstadoConciliacionBancaria.EnProceso;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Completar()
    {
        if (Estado != EstadoConciliacionBancaria.EnProceso) throw new InvalidOperationException("La conciliación debe estar en proceso para poder completarse.");
        if (_movimientos.Any(m => m.Estado == EstadoMovimientoEstadoCuenta.Pendiente || m.Estado == EstadoMovimientoEstadoCuenta.Parcial)) throw new InvalidOperationException("Todos los movimientos deben estar conciliados o ignorados.");
        if (Diferencia != 0) throw new InvalidOperationException("No se puede completar la conciliación si existe diferencia entre el saldo final y el conciliado.");
        Estado = EstadoConciliacionBancaria.Completada;
        FechaActualizacion = DateTime.UtcNow;
    }

    // La reapertura crea una nueva versión, sin borrar movimientos ni matches históricos.
    // La anulación ordinaria de un período completado sigue prohibida.
    public ConciliacionBancaria ReabrirComoBorrador(string motivo)
    {
        if (Estado != EstadoConciliacionBancaria.Completada)
            throw new InvalidOperationException("Solo se puede reabrir la última versión completada.");
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 500)
            throw new ArgumentException("Se requiere un motivo de reapertura de hasta 500 caracteres.", nameof(motivo));

        var nueva = new ConciliacionBancaria(CuentaBancariaId, FechaInicio, FechaFin,
            SaldoInicialBanco, SaldoFinalBanco, $"Reapertura de {Id}");
        // El motivo se registra en la auditoría estricta de la transacción.
        // Preservar las observaciones y los movimientos originales.
        Estado = EstadoConciliacionBancaria.Anulada;
        FechaActualizacion = DateTime.UtcNow;
        return nueva;
    }

    public void Anular(string motivo)
    {
        if (Estado == EstadoConciliacionBancaria.Completada) throw new InvalidOperationException("No se puede anular una conciliación ya completada.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("El motivo de anulación es requerido.", nameof(motivo));
        Observaciones = $"Anulada: {motivo}. {Observaciones}";
        Estado = EstadoConciliacionBancaria.Anulada;
        FechaActualizacion = DateTime.UtcNow;
    }
}
