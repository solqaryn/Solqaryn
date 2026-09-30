using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Application.DTOs.Bancos;

public sealed class CuentaBancariaDto
{
    public int Id { get; init; }
    public int BancoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string NumeroCuenta { get; init; } = string.Empty;
    public string Moneda { get; init; } = string.Empty;
    public decimal SaldoInicial { get; init; }
    public EstadoCuentaBancaria Estado { get; init; }
}

public sealed class OperacionBancariaDto
{
    public TipoOperacionBancaria TipoOperacion { get; init; }
    public decimal Monto { get; init; }
    public int? CuentaDestinoId { get; init; }
    public string Referencia { get; init; } = string.Empty;
}
