namespace Solqaryn.Application.DTOs.Bancos;

public sealed record ConciliacionBancariaFilterDto
{
    public int? CuentaBancariaId { get; init; }
    public Solqaryn.Domain.Enums.Bancos.EstadoConciliacionBancaria? Estado { get; init; }
    public int? Mes { get; init; }
    public int? Anio { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record ConciliacionBancariaPageDto
{
    public int TotalRecords { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public IEnumerable<ConciliacionBancariaResumenDto> Items { get; init; } = Array.Empty<ConciliacionBancariaResumenDto>();
}

public sealed record ConciliacionBancariaResumenDto
{
    public int Id { get; init; }
    public int CuentaBancariaId { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    public decimal SaldoInicialBanco { get; init; }
    public decimal SaldoFinalBanco { get; init; }
    public decimal SaldoConciliado { get; init; }
    public decimal Diferencia { get; init; }
}
