using System.ComponentModel.DataAnnotations;

namespace Solqaryn.Application.DTOs;

public class CreateCotizacionDto
{
    [Required]
    public int ClienteId { get; set; }
    public string? Observaciones { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "La cotización debe contener al menos un detalle.")]
    public List<CreateCotizacionDetalleDto> Detalles { get; set; } = new();
}

public class CreateCotizacionDetalleDto
{
    [Required]
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }

    [Required]
    [Range(0.0001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public decimal Cantidad { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El precio unitario no puede ser negativo.")]
    public decimal PrecioUnitario { get; set; }
}
