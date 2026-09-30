using Solqaryn.Domain.Enums.Cajas;

namespace Solqaryn.Application.DTOs;

public sealed class CajaDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public EstadoCaja Estado { get; init; }
    public int? SesionActivaId { get; init; }
}

public sealed class CajaSesionDto
{
    public int Id { get; init; }
    public int CajaId { get; init; }
    public int UsuarioId { get; init; }
    public DateTime FechaApertura { get; init; }
    public DateTime? FechaCierre { get; init; }
    public EstadoCajaSesion Estado { get; init; }
    public decimal FondoInicial { get; init; }
    public decimal TotalIngresos { get; init; }
    public decimal TotalRetiros { get; init; }
    public decimal TotalDepositos { get; init; }
    public decimal? SaldoEsperado { get; init; }
    public decimal? SaldoContado { get; init; }
    public decimal? Diferencia { get; init; }
    public string? ObservacionesArqueo { get; init; }
    public IReadOnlyList<CajaMovimientoDto> Movimientos { get; init; } = Array.Empty<CajaMovimientoDto>();
}

public sealed class CajaMovimientoDto
{
    public int Id { get; init; }
    public int CajaSesionId { get; init; }
    public int UsuarioId { get; init; }
    public TipoMovimientoCaja Tipo { get; init; }
    public decimal Monto { get; init; }
    public string Referencia { get; init; } = string.Empty;
    public DateTime FechaOperacion { get; init; }
    public decimal ImpactoSaldo { get; init; }
}

public sealed class CrearCajaDto
{
    public string Nombre { get; init; } = string.Empty;
}

public sealed class AbrirCajaSesionDto
{
    public decimal FondoInicial { get; init; }
}

public sealed class RegistrarMovimientoCajaDto
{
    public TipoMovimientoCaja Tipo { get; init; }
    public decimal Monto { get; init; }
    public string Referencia { get; init; } = string.Empty;
}

public sealed class CerrarCajaSesionDto
{
    public decimal SaldoContado { get; init; }
    public string? Observaciones { get; init; }
}
