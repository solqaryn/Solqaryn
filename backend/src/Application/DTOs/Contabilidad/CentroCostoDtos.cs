using Solqaryn.Domain.Entities.Contabilidad;

namespace Solqaryn.Application.DTOs.Contabilidad;

public class CentroCostoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoCentroCosto Tipo { get; set; }
    public int? SucursalId { get; set; }
    public string? SucursalCodigo { get; set; }
    public string? SucursalNombre { get; set; }
    public bool Activo { get; set; }
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public string? ActualizadoPorNombreUsuario { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class CreateCentroCostoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoCentroCosto Tipo { get; set; } = TipoCentroCosto.Sucursal;
    public int? SucursalId { get; set; }
}

public class UpdateCentroCostoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoCentroCosto Tipo { get; set; }
    public int? SucursalId { get; set; }
    public bool Activo { get; set; }
}

public class TipoCentroCostoDto
{
    public TipoCentroCosto Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
