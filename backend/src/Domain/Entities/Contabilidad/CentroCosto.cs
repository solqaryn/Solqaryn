using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Domain.Entities.Contabilidad;

/// <summary>
/// Clasificación funcional de un centro de costo. Solo Sucursal tiene hoy un
/// agregado persistente real; los demás tipos son categorías válidas sin FK
/// ficticia hasta que sus agregados existan en el dominio.
/// </summary>
public enum TipoCentroCosto
{
    Sucursal = 1,
    Departamento = 2,
    Proyecto = 3,
    UnidadNegocio = 4
}

/// <summary>
/// Unidad de clasificación de costos. Mantiene la asociación con Sucursal
/// fail-closed y evita referencias inventadas a agregados aún inexistentes.
/// </summary>
public class CentroCosto : AuditableEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TipoCentroCosto Tipo { get; set; } = TipoCentroCosto.Sucursal;

    /// <summary>
    /// Única asociación estructural confirmada actualmente. Es obligatoria
    /// cuando Tipo=Sucursal y debe permanecer nula para los demás tipos.
    /// </summary>
    public int? SucursalId { get; set; }
    public Sucursal? Sucursal { get; set; }

    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public bool TieneAsociacionValida()
        => Tipo == TipoCentroCosto.Sucursal
            ? SucursalId.HasValue
            : !SucursalId.HasValue;

    public void Activar()
    {
        Activo = true;
        Eliminado = false;
        FechaEliminacion = null;
        EliminadoPorUsuarioId = null;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Desactivar()
    {
        Activo = false;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void MarcarEliminado(int? usuarioId)
    {
        Activo = false;
        Eliminado = true;
        FechaEliminacion = DateTime.UtcNow;
        EliminadoPorUsuarioId = usuarioId;
        FechaActualizacion = DateTime.UtcNow;
    }
}
