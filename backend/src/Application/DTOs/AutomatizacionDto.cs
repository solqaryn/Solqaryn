namespace Solqaryn.Application.DTOs;

public sealed class AutomatizacionConfiguracionDto
{
    public int DiasBorradorVentaAlerta { get; set; } = 2;
    public int DiasBorradorCompraAlerta { get; set; } = 7;
    public int DiasCargaPendienteAlerta { get; set; } = 1;
    public int DiasMovimientoFinancieroPendienteAlerta { get; set; } = 7;
    public int LimiteSugerencias { get; set; } = 20;
    public int LimiteAutocompletado { get; set; } = 10;
    public bool MostrarRecordatoriosDashboard { get; set; } = true;
    public string VersionReglas { get; set; } = "M12.1";
    public DateTime? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
}

public sealed class ActualizarAutomatizacionConfiguracionRequest
{
    public int DiasBorradorVentaAlerta { get; set; }
    public int DiasBorradorCompraAlerta { get; set; }
    public int DiasCargaPendienteAlerta { get; set; }
    public int DiasMovimientoFinancieroPendienteAlerta { get; set; }
    public int LimiteSugerencias { get; set; }
    public int LimiteAutocompletado { get; set; }
    public bool MostrarRecordatoriosDashboard { get; set; }
}

public sealed class AutomatizacionSugerenciaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Severidad { get; set; } = "Info";
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public bool RequiereConfirmacion { get; set; } = true;
}

public sealed class AutomatizacionResumenDto
{
    public string VersionReglas { get; set; } = "M12.1";
    public DateTime GeneradoEnUtc { get; set; }
    public int TotalSugerencias { get; set; }
    public List<AutomatizacionSugerenciaDto> Sugerencias { get; set; } = new();
}

public sealed class AutocompletadoItemDto
{
    public int Id { get; set; }
    public string Contexto { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string? Codigo { get; set; }
}

public sealed class AccionMasivaPreviewRequest
{
    public string Accion { get; set; } = string.Empty;
    public List<int> Ids { get; set; } = new();
}

public sealed class AccionMasivaPreviewDto
{
    public string Accion { get; set; } = string.Empty;
    public int Solicitados { get; set; }
    public int Aplicables { get; set; }
    public int Omitidos { get; set; }
    public bool SoloVistaPrevia { get; set; } = true;
    public bool RequiereConfirmacion { get; set; } = true;
    public List<int> IdsAplicables { get; set; } = new();
    public List<string> Advertencias { get; set; } = new();
}
