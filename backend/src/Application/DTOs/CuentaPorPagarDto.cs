using System.ComponentModel.DataAnnotations;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class CuentaPorPagarDto
{
    public int Id { get; set; }
    public int FacturaProveedorId { get; set; }
    public int ProveedorId { get; set; }
    public string Moneda { get; set; } = "HNL";
    public CondicionPagoProveedor CondicionPago { get; set; }
    public DateTime FechaEmisionUtc { get; set; }
    public DateTime FechaVencimientoUtc { get; set; }
    public decimal MontoOriginal { get; set; }
    public decimal MontoAplicado { get; set; }
    public decimal Saldo { get; set; }
    public EstadoCuentaPorPagar Estado { get; set; }
    public DateTime? FechaAnulacionUtc { get; set; }
    public string? MotivoAnulacion { get; set; }
    public IReadOnlyList<AplicacionCuentaPorPagarDto> Aplicaciones { get; set; } = Array.Empty<AplicacionCuentaPorPagarDto>();
}

public sealed class AplicacionCuentaPorPagarDto
{
    public int Id { get; set; }
    public TipoAplicacionCuentaPorPagar Tipo { get; set; }
    public decimal Monto { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? ReferenciaExterna { get; set; }
    public DateTime FechaAplicacionUtc { get; set; }
    public bool Revertida { get; set; }
    public DateTime? FechaReversionUtc { get; set; }
    public string? MotivoReversion { get; set; }
}

public sealed class GenerarCuentaPorPagarDto
{
    [Range(1, int.MaxValue)]
    public int FacturaProveedorId { get; set; }

    [Required]
    public CondicionPagoProveedor CondicionPago { get; set; }

    public DateTime? FechaVencimientoUtc { get; set; }
}

public sealed class AplicarCuentaPorPagarDto
{
    [Required]
    public TipoAplicacionCuentaPorPagar Tipo { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Monto { get; set; }

    [Required, StringLength(128, MinimumLength = 1)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ReferenciaExterna { get; set; }

    [Required]
    public DateTime FechaAplicacionUtc { get; set; }
}

public sealed class RevertirAplicacionCuentaPorPagarDto
{
    [Required, StringLength(128, MinimumLength = 1)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    public DateTime FechaReversionUtc { get; set; }
}

public sealed class AnularCuentaPorPagarDto
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string Motivo { get; set; } = string.Empty;

    [Required]
    public DateTime FechaAnulacionUtc { get; set; }
}
