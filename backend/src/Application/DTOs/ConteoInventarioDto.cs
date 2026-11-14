using System.ComponentModel.DataAnnotations;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class ConteoInventarioDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public TipoConteoInventario Tipo { get; set; }
    public string TipoNombre => Tipo.ToString();
    public EstadoConteoInventario Estado { get; set; }
    public string EstadoNombre => Estado.ToString();
    public int AlmacenId { get; set; }
    public string? AlmacenNombre { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public string? UbicacionNombre { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public bool EsCiego { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaInicio { get; set; }
    public int? IniciadoPorUsuarioId { get; set; }
    public DateTime? FechaCierre { get; set; }
    public int? CerradoPorUsuarioId { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public int? AprobadoPorUsuarioId { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public int? CanceladoPorUsuarioId { get; set; }
    public string? MotivoCancelacion { get; set; }
    public int CantidadLineas { get; set; }
    public int CantidadCapturadas { get; set; }
    public int CantidadConDiferencia { get; set; }
    public int DiferenciaNeta { get; set; }
    public List<ConteoInventarioDetalleDto> Detalles { get; set; } = new();
}

public class ConteoInventarioDetalleDto
{
    public int Id { get; set; }
    public int ConteoInventarioId { get; set; }
    public int ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int? StockEsperado { get; set; }
    public int? CantidadContada { get; set; }
    public int? Diferencia { get; set; }
    public DateTime? FechaConteo { get; set; }
    public int? ContadoPorUsuarioId { get; set; }
    public int? AjusteInventarioId { get; set; }
    public string? ProductoSku { get; set; }
    public string? ProductoMarca { get; set; }
    public string? ProductoModelo { get; set; }
    public string? ProductoColor { get; set; }
    public string? ProductoTalla { get; set; }
}

public class CreateConteoInventarioDto
{
    public TipoConteoInventario Tipo { get; set; } = TipoConteoInventario.General;
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int? CategoriaId { get; set; }
    public bool EsCiego { get; set; }
    public string? Observaciones { get; set; }
    [Required]
    public List<int> ProductoVarianteIds { get; set; } = new();
}

public class UpdateConteoInventarioDto
{
    public TipoConteoInventario Tipo { get; set; } = TipoConteoInventario.General;
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int? CategoriaId { get; set; }
    public bool EsCiego { get; set; }
    public string? Observaciones { get; set; }
    [Required]
    public List<int> ProductoVarianteIds { get; set; } = new();
}

public class CapturarConteoInventarioDetalleDto
{
    [Range(0, int.MaxValue)]
    public int CantidadContada { get; set; }
}

public class CapturarConteoInventarioLoteDto
{
    [Required]
    public List<CapturaConteoInventarioLineaDto> Lineas { get; set; } = new();
}

public class CapturaConteoInventarioLineaDto
{
    [Range(1, int.MaxValue)]
    public int DetalleId { get; set; }

    [Range(0, int.MaxValue)]
    public int CantidadContada { get; set; }
}

public class CancelarConteoInventarioDto
{
    public string Motivo { get; set; } = string.Empty;
}

public class ConteoInventarioQueryDto : IValidatableObject
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int? CategoriaId { get; set; }
    public TipoConteoInventario? Tipo { get; set; }
    public EstadoConteoInventario? Estado { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Desde.HasValue && Hasta.HasValue && Desde.Value > Hasta.Value)
        {
            yield return new ValidationResult(
                "Desde no puede ser posterior a Hasta.",
                new[] { nameof(Desde), nameof(Hasta) });
        }
    }
}

public class ConteoInventarioResumenDto
{
    public int ConteoInventarioId { get; set; }
    public int TotalLineas { get; set; }
    public int Capturadas { get; set; }
    public int Pendientes { get; set; }
    public int ConDiferencia { get; set; }
    public int DiferenciaNeta { get; set; }
    public bool PuedeCerrar => TotalLineas > 0 && Pendientes == 0;
}
