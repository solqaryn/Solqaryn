using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Domain.Entities.Bancos;

/// <summary>
/// Cuenta operativa asociada al catálogo Banco. Esta entidad define identidad,
/// estado e invariantes de operación; no mantiene un ledger paralelo.
/// MovimientoFinanciero continúa siendo la autoridad de movimientos financieros.
/// </summary>
public class CuentaBancaria : AuditableEntity
{
    public int BancoId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string NumeroCuenta { get; private set; } = null!;
    public string Moneda { get; private set; } = null!;
    public decimal SaldoInicial { get; private set; }
    public EstadoCuentaBancaria Estado { get; private set; }

    protected CuentaBancaria() { }

    public CuentaBancaria(
        int bancoId,
        string nombre,
        string numeroCuenta,
        string moneda,
        decimal saldoInicial = 0m)
    {
        if (bancoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bancoId), "El banco debe estar persistido.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la cuenta es requerido.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(numeroCuenta))
            throw new ArgumentException("El número de cuenta es requerido.", nameof(numeroCuenta));
        if (string.IsNullOrWhiteSpace(moneda))
            throw new ArgumentException("La moneda es requerida.", nameof(moneda));
        if (saldoInicial < 0m)
            throw new ArgumentOutOfRangeException(nameof(saldoInicial), "El saldo inicial no puede ser negativo.");

        BancoId = bancoId;
        Nombre = nombre.Trim();
        NumeroCuenta = numeroCuenta.Trim();
        Moneda = moneda.Trim().ToUpperInvariant();
        SaldoInicial = saldoInicial;
        Estado = EstadoCuentaBancaria.Activa;
    }

    public void Activar()
    {
        Estado = EstadoCuentaBancaria.Activa;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Desactivar()
    {
        Estado = EstadoCuentaBancaria.Inactiva;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void UpdateNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la cuenta es requerido.", nameof(nombre));

        Nombre = nombre.Trim();
        FechaActualizacion = DateTime.UtcNow;
    }

    /// <summary>
    /// Valida el contrato de una operación bancaria sin registrar movimientos ni
    /// duplicar el ledger. La capa de aplicación debe materializar la operación
    /// mediante MovimientoFinanciero y persistencia transaccional.
    /// </summary>
    public void ValidarOperacion(
        TipoOperacionBancaria tipo,
        decimal monto,
        int? cuentaDestinoId = null)
    {
        if (Estado != EstadoCuentaBancaria.Activa)
            throw new InvalidOperationException("La cuenta bancaria debe estar activa para operar.");
        if (monto <= 0m)
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser mayor que cero.");

        if (tipo == TipoOperacionBancaria.Transferencia)
        {
            if (!cuentaDestinoId.HasValue || cuentaDestinoId.Value <= 0)
                throw new ArgumentException("Una transferencia requiere una cuenta destino persistida.", nameof(cuentaDestinoId));
            if (Id > 0 && cuentaDestinoId.Value == Id)
                throw new InvalidOperationException("La cuenta origen y destino de una transferencia deben ser distintas.");
        }
        else if (cuentaDestinoId.HasValue)
        {
            throw new ArgumentException("Solo una transferencia admite cuenta destino.", nameof(cuentaDestinoId));
        }
    }
}
