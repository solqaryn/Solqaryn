using System.ComponentModel.DataAnnotations;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class CuentaContableDto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public TipoCuentaContable Tipo { get; init; }
    public int? CuentaPadreId { get; init; }
    public bool AceptaMovimientos { get; init; }
    public bool Activa { get; init; }
    public bool EsRaiz { get; init; }
    public List<CuentaContableDto> Subcuentas { get; init; } = new();
}

public sealed class CreateCuentaContableDto
{
    [Required, MinLength(1), MaxLength(50)]
    public string Codigo { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(200)]
    public string Nombre { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Descripcion { get; init; }

    [Required]
    public TipoCuentaContable Tipo { get; init; }

    public int? CuentaPadreId { get; init; }
    public bool AceptaMovimientos { get; init; } = true;
    public bool Activa { get; init; } = true;
}

public sealed class UpdateCuentaContableDto
{
    [Required, MinLength(1), MaxLength(50)]
    public string Codigo { get; init; } = string.Empty;

    [Required, MinLength(1), MaxLength(200)]
    public string Nombre { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Descripcion { get; init; }

    [Required]
    public TipoCuentaContable Tipo { get; init; }

    public int? CuentaPadreId { get; init; }
    public bool AceptaMovimientos { get; init; }
    public bool Activa { get; init; }
}
