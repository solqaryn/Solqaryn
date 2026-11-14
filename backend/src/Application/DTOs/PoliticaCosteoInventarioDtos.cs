using System.ComponentModel.DataAnnotations;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public sealed class PoliticaCosteoInventarioQueryDto
{
    public MetodoCosteoInventario? Metodo { get; set; }
    public bool? Vigente { get; set; }
    public DateTime? DesdeUtc { get; set; }
    public DateTime? HastaUtc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class CambiarPoliticaCosteoInventarioDto
{
    [Required]
    public MetodoCosteoInventario Metodo { get; set; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(500, MinimumLength = 3)]
    public string Motivo { get; set; } = string.Empty;
}

public sealed class PoliticaCosteoInventarioDto
{
    public int Id { get; set; }
    public int EmpresaConfiguracionId { get; set; }
    public MetodoCosteoInventario Metodo { get; set; }
    public string MetodoNombre { get; set; } = string.Empty;
    public DateTime VigenteDesdeUtc { get; set; }
    public DateTime? VigenteHastaUtc { get; set; }
    public bool EstaVigente { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public sealed class MetodoCosteoInventarioDto
{
    public MetodoCosteoInventario Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
