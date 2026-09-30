using Solqaryn.Application.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class ConfiguracionTrazabilidadVarianteDto
{
    public int ProductoVarianteId { get; set; }
    public bool ControlaLote { get; set; }
    public bool ControlaNumeroSerie { get; set; }
    public bool ControlaFechaVencimiento { get; set; }
    public int? DiasAlertaVencimiento { get; set; }
}

public class ConfigurarTrazabilidadVarianteRequest
{
    public bool ControlaLote { get; set; }
    public bool ControlaNumeroSerie { get; set; }
    public bool ControlaFechaVencimiento { get; set; }
    public int? DiasAlertaVencimiento { get; set; }
}

public class LoteInventarioDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime? FechaFabricacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool Activo { get; set; }
}

public class CrearLoteInventarioRequest
{
    public int ProductoVarianteId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public DateTime? FechaFabricacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}

public sealed class ActualizarLoteInventarioRequest
{
    public string Codigo { get; set; } = string.Empty;
    public DateTime? FechaFabricacion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}

public sealed class LoteInventarioQueryDto : PagedRequest
{
    public int? ProductoVarianteId { get; set; }
    public bool? Activo { get; set; }
    public DateTime? VenceDesde { get; set; }
    public DateTime? VenceHasta { get; set; }
    public bool? SoloVencidos { get; set; }
}

public class SerieInventarioDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int? LoteInventarioId { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
    public EstadoSerieInventario Estado { get; set; }
}

public class CrearSerieInventarioRequest
{
    public int ProductoVarianteId { get; set; }
    public int? LoteInventarioId { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
}

public sealed class SerieInventarioQueryDto : PagedRequest
{
    public int? ProductoVarianteId { get; set; }
    public int? LoteInventarioId { get; set; }
    public EstadoSerieInventario? Estado { get; set; }
}
