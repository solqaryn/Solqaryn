using System.ComponentModel.DataAnnotations;

namespace Solqaryn.Application.DTOs;

public sealed class ReservaInventarioDetalleDto
{
    public int Id { get; set; }
    public int ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public int CantidadReservada { get; set; }
    public int CantidadConsumida { get; set; }
    public string? ProductoSku { get; set; }
    public string? ProductoMarca { get; set; }
    public string? ProductoModelo { get; set; }
    public string? ProductoColor { get; set; }
    public string? ProductoTalla { get; set; }
}

public sealed class ReservaInventarioDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int? VentaId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaExpiracion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActivacion { get; set; }
    public DateTime? FechaConsumo { get; set; }
    public DateTime? FechaLiberacion { get; set; }
    public DateTime? FechaExpiracionAplicada { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public string? MotivoLiberacion { get; set; }
    public string? MotivoCancelacion { get; set; }
    public List<ReservaInventarioDetalleDto> Detalles { get; set; } = new();
}

public sealed class ReservaInventarioDetalleInputDto
{
    [Range(1, int.MaxValue)]
    public int ProductoVarianteId { get; set; }

    [Range(1, int.MaxValue)]
    public int AlmacenId { get; set; }

    [Range(1, int.MaxValue)]
    public int? UbicacionAlmacenId { get; set; }

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; }
}

public sealed class CreateReservaInventarioDto
{
    [Range(1, int.MaxValue)]
    public int? VentaId { get; set; }

    public DateTime? FechaExpiracion { get; set; }

    [Required]
    [MinLength(1)]
    public List<ReservaInventarioDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class UpdateReservaInventarioDto
{
    public DateTime? FechaExpiracion { get; set; }

    [Required]
    [MinLength(1)]
    public List<ReservaInventarioDetalleInputDto> Detalles { get; set; } = new();
}

public sealed class LiberarReservaInventarioDto
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;
}

public sealed class CancelarReservaInventarioDto
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;
}

public sealed class ReservaInventarioQueryDto : IValidatableObject
{
    public string? Busqueda { get; set; }
    public string? Estado { get; set; }

    [Range(1, int.MaxValue)]
    public int? VentaId { get; set; }

    [Range(1, int.MaxValue)]
    public int? AlmacenId { get; set; }

    public DateTime? ExpiraDesde { get; set; }
    public DateTime? ExpiraHasta { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpiraDesde.HasValue && ExpiraHasta.HasValue && ExpiraDesde.Value > ExpiraHasta.Value)
        {
            yield return new ValidationResult(
                "ExpiraDesde no puede ser posterior a ExpiraHasta.",
                new[] { nameof(ExpiraDesde), nameof(ExpiraHasta) });
        }
    }
}