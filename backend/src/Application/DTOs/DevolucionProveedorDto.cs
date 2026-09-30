using System.ComponentModel.DataAnnotations;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class DevolucionProveedorDto
{
    public int Id { get; set; }
    public string NumeroDevolucion { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public int OrdenCompraId { get; set; }
    public int RecepcionCompraId { get; set; }
    public int FacturaProveedorId { get; set; }
    public string ProveedorNombreSnapshot { get; set; } = string.Empty;
    public string Moneda { get; set; } = "HNL";
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public EstadoDevolucionProveedor Estado { get; set; }
    public DateTime? FechaConfirmacionUtc { get; set; }
    public int? ConfirmadaPorUsuarioId { get; set; }
    public string? ConfirmadaPorNombreSnapshot { get; set; }
    public DateTime? FechaAnulacionUtc { get; set; }
    public int? AnuladaPorUsuarioId { get; set; }
    public string? MotivoAnulacion { get; set; }
    public decimal SubtotalCredito { get; set; }
    public decimal ImpuestoCredito { get; set; }
    public decimal TotalCredito { get; set; }
    public List<DevolucionProveedorDetalleDto> Detalles { get; set; } = new();
}

public class DevolucionProveedorDetalleDto
{
    public int Id { get; set; }
    public int RecepcionCompraDetalleId { get; set; }
    public int OrdenCompraDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitarioSnapshot { get; set; }
    public decimal ImpuestoUnitarioSnapshot { get; set; }
    public decimal SubtotalCredito { get; set; }
    public decimal ImpuestoCredito { get; set; }
    public decimal TotalCredito { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string ProductoNombreSnapshot { get; set; } = string.Empty;
}

public class CreateDevolucionProveedorDto
{
    [Range(1, int.MaxValue)]
    public int RecepcionCompraId { get; set; }

    [Range(1, int.MaxValue)]
    public int FacturaProveedorId { get; set; }

    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<DevolucionProveedorDetalleInputDto> Detalles { get; set; } = new();
}

public class UpdateDevolucionProveedorDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<DevolucionProveedorDetalleInputDto> Detalles { get; set; } = new();
}

public class DevolucionProveedorDetalleInputDto
{
    [Range(1, int.MaxValue)]
    public int RecepcionCompraDetalleId { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Cantidad { get; set; }
}

public class AnularDevolucionProveedorDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;
}

public class DevolucionProveedorQueryDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? ProveedorId { get; set; }

    [Range(1, int.MaxValue)]
    public int? OrdenCompraId { get; set; }

    [Range(1, int.MaxValue)]
    public int? RecepcionCompraId { get; set; }

    [Range(1, int.MaxValue)]
    public int? FacturaProveedorId { get; set; }

    public EstadoDevolucionProveedor? Estado { get; set; }
    public DateTime? DesdeUtc { get; set; }
    public DateTime? HastaUtc { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DesdeUtc.HasValue && HastaUtc.HasValue && DesdeUtc.Value > HastaUtc.Value)
            yield return new ValidationResult("DesdeUtc no puede ser posterior a HastaUtc.", new[] { nameof(DesdeUtc), nameof(HastaUtc) });
    }
}
