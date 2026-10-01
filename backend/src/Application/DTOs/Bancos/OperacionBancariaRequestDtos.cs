namespace Solqaryn.Application.DTOs.Bancos;

public abstract record OperacionBancariaBaseDto
{
    public int CuentaId { get; init; }
    public decimal Monto { get; init; }
    public string Referencia { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed record DepositoBancarioDto : OperacionBancariaBaseDto;
public sealed record RetiroBancarioDto : OperacionBancariaBaseDto;
public sealed record ComisionBancariaDto : OperacionBancariaBaseDto;
public sealed record InteresBancarioDto : OperacionBancariaBaseDto;
public sealed record ConciliacionBancariaDto : OperacionBancariaBaseDto;

public sealed record TransferenciaBancariaDto : OperacionBancariaBaseDto
{
    public int CuentaDestinoId { get; init; }
}
