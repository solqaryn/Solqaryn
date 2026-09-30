using System.ComponentModel.DataAnnotations;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class RecepcionCompraDto
{
    public int Id { get; set; }
    public string NumeroRecepcion { get; set; } = string.Empty;
    public int OrdenCompraId { get; set; }
    public string? NumeroOrdenCompra { get; set; }
    public EstadoRecepcionCompra Estado { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaRecepcionUtc { get; set; }
    public int? RecibidaPorUsuarioId { get; set; }
    public string? RecibidaPorNombreSnapshot { get; set; }
    public DateTime? FechaAnulacionUtc { get; set; }
    public int? AnuladaPorUsuarioId { get; set; }
    public string? MotivoAnulacion { get; set; }
    public decimal CantidadRecibidaTotal { get; set; }
    public decimal CantidadAceptadaTotal { get; set; }
    public decimal CantidadDanadaTotal { get; set; }
    public decimal CantidadFaltanteTotal { get; set; }
    public decimal CantidadSobranteTotal { get; set; }
    public List<RecepcionCompraDetalleDto> Detalles { get; set; } = new();
}

public class RecepcionCompraDetalleDto
{
    public int Id { get; set; }
    public int OrdenCompraDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int? UbicacionAlmacenId { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal CantidadAceptada { get; set; }
    public decimal CantidadDanada { get; set; }
    public decimal CantidadFaltante { get; set; }
    public decimal CantidadSobrante { get; set; }
    public decimal CostoUnitarioSnapshot { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }
}

public class RecepcionCompraSaldoOrdenDto
{
    public int OrdenCompraId { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public EstadoOrdenCompra EstadoOrden { get; set; }
    public List<RecepcionCompraSaldoLineaDto> Lineas { get; set; } = new();
    public bool Completa => Lineas.Count > 0 && Lineas.All(x => x.CantidadPendiente <= 0m);
}

public class RecepcionCompraSaldoLineaDto
{
    public int OrdenCompraDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoVarianteId { get; set; }
    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoNombreSnapshot { get; set; }
    public decimal CantidadOrdenada { get; set; }
    public decimal CantidadAceptadaAcumulada { get; set; }
    public decimal CantidadPendiente { get; set; }
}

public class CreateRecepcionCompraDto
{
    [Range(1, int.MaxValue)]
    public int OrdenCompraId { get; set; }

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<RecepcionCompraDetalleInputDto> Detalles { get; set; } = new();
}

public class UpdateRecepcionCompraDto
{
    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Required, MinLength(1)]
    public List<RecepcionCompraDetalleInputDto> Detalles { get; set; } = new();
}

public class RecepcionCompraDetalleInputDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int OrdenCompraDetalleId { get; set; }

    [Range(1, int.MaxValue)]
    public int AlmacenId { get; set; }

    [Range(1, int.MaxValue)]
    public int? UbicacionAlmacenId { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal CantidadRecibida { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal CantidadDanada { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal CantidadFaltante { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal CantidadSobrante { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CantidadDanada + CantidadSobrante > CantidadRecibida)
        {
            yield return new ValidationResult(
                "Las cantidades dañada y sobrante no pueden superar conjuntamente la cantidad físicamente recibida.",
                new[] { nameof(CantidadRecibida), nameof(CantidadDanada), nameof(CantidadSobrante) });
        }

        if (CantidadRecibida == 0m && CantidadFaltante == 0m)
        {
            yield return new ValidationResult(
                "El detalle debe registrar recepción física o faltante.",
                new[] { nameof(CantidadRecibida), nameof(CantidadFaltante) });
        }
    }
}

public class AnularRecepcionCompraDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;
}

public class RecepcionCompraQueryDto : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? OrdenCompraId { get; set; }
    public EstadoRecepcionCompra? Estado { get; set; }
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
