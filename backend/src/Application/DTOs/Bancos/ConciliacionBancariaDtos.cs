namespace Solqaryn.Application.DTOs.Bancos;

public sealed record ImportarEstadoCuentaRequestDto
{
    public int CuentaBancariaId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public IEnumerable<MovimientoEstadoCuentaDto> Movimientos { get; init; } = Array.Empty<MovimientoEstadoCuentaDto>();
}

public sealed record MovimientoEstadoCuentaDto
{
    public DateTime FechaOperacion { get; init; }
    public decimal Monto { get; init; }
    public string ReferenciaExterna { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public string IdentificadorExternoTransaccion { get; init; } = string.Empty;
}

public sealed record ImportarEstadoCuentaResponseDto
{
    public int CuentaBancariaId { get; init; }
    public int MovimientosImportados { get; init; }
    public int MovimientosDuplicadosIgnorados { get; init; }
    public IEnumerable<string> Errores { get; init; } = Array.Empty<string>();
}

public sealed record ConciliarMovimientosRequestDto
{
    public int CuentaBancariaId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public IEnumerable<MatchConciliacionDto> Matches { get; init; } = Array.Empty<MatchConciliacionDto>();
}

public sealed record MatchConciliacionDto
{
    public int MovimientoInternoId { get; init; }
    public string IdentificadorExternoTransaccion { get; init; } = string.Empty;
}

public sealed record ConciliarMovimientosResponseDto
{
    public int MatchesExitosos { get; init; }
    public IEnumerable<string> Errores { get; init; } = Array.Empty<string>();
}

public sealed record DiferenciaConciliacionDto
{
    public string IdentificadorExternoTransaccion { get; init; } = string.Empty;
    public decimal DiferenciaMonto { get; init; }
    public string Motivo { get; init; } = string.Empty;
}

public sealed record SolicitarAjusteRequestDto
{
    public int CuentaBancariaId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public IEnumerable<DiferenciaConciliacionDto> Diferencias { get; init; } = Array.Empty<DiferenciaConciliacionDto>();
}

public sealed record SolicitarAjusteResponseDto
{
    public int AjustesSolicitados { get; init; }
    public IEnumerable<string> Errores { get; init; } = Array.Empty<string>();
}

public sealed record CerrarPeriodoConciliacionRequestDto
{
    public int CuentaBancariaId { get; init; }
    public int Mes { get; init; }
    public int Anio { get; init; }
    public decimal SaldoFinalEstadoCuenta { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed record CerrarPeriodoConciliacionResponseDto
{
    public bool Exitoso { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public IEnumerable<string> DiferenciasPendientes { get; init; } = Array.Empty<string>();
}

public sealed record ReabrirPeriodoConciliacionRequestDto
{
    public int CuentaBancariaId { get; init; }
    public int Mes { get; init; }
    public int Anio { get; init; }
    public string MotivoReapertura { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
}

public sealed record ReabrirPeriodoConciliacionResponseDto
{
    public bool Exitoso { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}
