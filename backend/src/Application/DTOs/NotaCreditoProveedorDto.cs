using System.ComponentModel.DataAnnotations;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class NotaCreditoProveedorDto
{
    public int Id { get; set; }
    public string NumeroNotaCredito { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public int FacturaProveedorId { get; set; }
    public int? DevolucionProveedorId { get; set; }
    public string ProveedorNombreSnapshot { get; set; } = string.Empty;
    public string Moneda { get; set; } = "HNL";
    public DateTime FechaEmisionUtc { get; set; }
    public string? ReferenciaFiscal { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public decimal SubtotalCredito { get; set; }
    public decimal ImpuestoCredito { get; set; }
    public decimal TotalCredito { get; set; }
    public EstadoNotaCreditoProveedor Estado { get; set; }
    public DateTime? FechaRegistroUtc { get; set; }
    public int? RegistradaPorUsuarioId { get; set; }
    public string? RegistradaPorNombreSnapshot { get; set; }
    public DateTime? FechaAnulacionUtc { get; set; }
    public int? AnuladaPorUsuarioId { get; set; }
    public string? MotivoAnulacion { get; set; }
}

public class CreateNotaCreditoProveedorDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string NumeroNotaCredito { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int FacturaProveedorId { get; set; }

    [Range(1, int.MaxValue)]
    public int? DevolucionProveedorId { get; set; }

    [Required]
    public DateTime FechaEmisionUtc { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Moneda { get; set; } = "HNL";

    [StringLength(120)]
    public string? ReferenciaFiscal { get; set; }

    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal SubtotalCredito { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal ImpuestoCredito { get; set; }
}

public class UpdateNotaCreditoProveedorDto
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string NumeroNotaCredito { get; set; } = string.Empty;

    [Required]
    public DateTime FechaEmisionUtc { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Moneda { get; set; } = "HNL";

    [StringLength(120)]
    public string? ReferenciaFiscal { get; set; }

    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Observaciones { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal SubtotalCredito { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal ImpuestoCredito { get; set; }
}

public class AnularNotaCreditoProveedorDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;
}
