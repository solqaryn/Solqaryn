namespace Solqaryn.Application.DTOs.Bancos;

public sealed class CreateCuentaBancariaDto
{
    public int BancoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string NumeroCuenta { get; init; } = string.Empty;
    public string Moneda { get; init; } = string.Empty;
    public decimal SaldoInicial { get; init; }
}
