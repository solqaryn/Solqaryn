using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums.Cajas;

namespace Solqaryn.Domain.Entities.Cajas;

public class CajaSesion : BaseEntity
{
    public int CajaId { get; private set; }
    public int UsuarioId { get; private set; }
    public DateTime FechaApertura { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public EstadoCajaSesion Estado { get; private set; }
    public decimal FondoInicial { get; private set; }
    public decimal TotalIngresos { get; private set; }
    public decimal TotalRetiros { get; private set; }
    public decimal TotalDepositos { get; private set; }
    public decimal? SaldoEsperado { get; private set; }
    public decimal? SaldoContado { get; private set; }
    public decimal? Diferencia { get; private set; }
    public string? ObservacionesArqueo { get; private set; }

    private readonly List<CajaMovimiento> _movimientos = new();
    public IReadOnlyCollection<CajaMovimiento> Movimientos => _movimientos.AsReadOnly();

    protected CajaSesion() { }

    public CajaSesion(int cajaId, int usuarioId, decimal fondoInicial)
    {
        if (cajaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(cajaId));
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId));
        if (fondoInicial < 0)
            throw new ArgumentOutOfRangeException(nameof(fondoInicial));

        CajaId = cajaId;
        UsuarioId = usuarioId;
        FondoInicial = fondoInicial;
        Estado = EstadoCajaSesion.Apertura;
        FechaApertura = DateTime.UtcNow;
    }

    public void IniciarOperaciones()
    {
        if (Estado != EstadoCajaSesion.Apertura)
            throw new InvalidOperationException("Solo una sesión en apertura puede iniciar operaciones.");

        Estado = EstadoCajaSesion.Operaciones;
        FechaActualizacion = DateTime.UtcNow;
    }

    public CajaMovimiento RegistrarMovimiento(TipoMovimientoCaja tipo, decimal monto, string referencia)
    {
        if (Estado != EstadoCajaSesion.Operaciones)
            throw new InvalidOperationException("Solo se registran movimientos durante Operaciones.");
        if (Id <= 0)
            throw new InvalidOperationException("La sesión debe estar persistida antes de registrar movimientos.");

        if (tipo is TipoMovimientoCaja.DiferenciaSobrante or TipoMovimientoCaja.DiferenciaFaltante)
            throw new InvalidOperationException("Las diferencias solo se registran durante el cierre de arqueo.");

        if (tipo is not TipoMovimientoCaja.Ingreso
            and not TipoMovimientoCaja.Retiro
            and not TipoMovimientoCaja.DepositoBanco)
            throw new InvalidOperationException("Tipo de movimiento no soportado.");

        var movimiento = new CajaMovimiento(Id, UsuarioId, tipo, monto, referencia);
        _movimientos.Add(movimiento);

        switch (tipo)
        {
            case TipoMovimientoCaja.Ingreso:
                TotalIngresos += monto;
                break;
            case TipoMovimientoCaja.Retiro:
                TotalRetiros += monto;
                break;
            case TipoMovimientoCaja.DepositoBanco:
                TotalDepositos += monto;
                break;
        }

        FechaActualizacion = DateTime.UtcNow;
        return movimiento;
    }

    public void IniciarArqueo()
    {
        if (Estado != EstadoCajaSesion.Operaciones)
            throw new InvalidOperationException("Solo una sesión en operaciones puede iniciar arqueo.");

        SaldoEsperado = FondoInicial + TotalIngresos - TotalRetiros - TotalDepositos;
        Estado = EstadoCajaSesion.Arqueo;
        FechaActualizacion = DateTime.UtcNow;
    }

    public CajaMovimiento? Cerrar(decimal saldoContado, string? observaciones = null)
    {
        if (Estado != EstadoCajaSesion.Arqueo || !SaldoEsperado.HasValue)
            throw new InvalidOperationException("La sesión debe estar en arqueo para cerrarse.");
        if (saldoContado < 0)
            throw new ArgumentOutOfRangeException(nameof(saldoContado));
        if (Id <= 0)
            throw new InvalidOperationException("La sesión debe estar persistida antes de cerrarse.");

        SaldoContado = saldoContado;
        Diferencia = saldoContado - SaldoEsperado.Value;
        ObservacionesArqueo = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();

        CajaMovimiento? diferenciaMovimiento = null;
        if (Diferencia.Value != 0)
        {
            var tipo = Diferencia.Value > 0
                ? TipoMovimientoCaja.DiferenciaSobrante
                : TipoMovimientoCaja.DiferenciaFaltante;
            diferenciaMovimiento = new CajaMovimiento(
                Id,
                UsuarioId,
                tipo,
                Math.Abs(Diferencia.Value),
                Diferencia.Value > 0 ? "Sobrante de arqueo" : "Faltante de arqueo");
            _movimientos.Add(diferenciaMovimiento);
        }

        Estado = EstadoCajaSesion.Cerrada;
        FechaCierre = DateTime.UtcNow;
        FechaActualizacion = DateTime.UtcNow;
        return diferenciaMovimiento;
    }
}
