using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Ubicación física/lógica dentro de un Almacén. ERP-N1.3 modela únicamente
/// topología interna; las existencias por ubicación pertenecen a ERP-N1.4.
/// El ownership de Empresa se deriva únicamente por Almacen -> Sucursal y no
/// se duplica en esta entidad.
/// </summary>
public class UbicacionAlmacen : AuditableEntity
{
    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;

    /// <summary>
    /// Resuelve transitivamente la Empresa propietaria por la jerarquía canónica.
    /// </summary>
    public int ObtenerEmpresaIdTenant()
    {
        if (Almacen is null)
        {
            throw new InvalidOperationException("La ubicación no tiene un Almacén cargado para resolver su Empresa tenant.");
        }

        return Almacen.ObtenerEmpresaIdTenant();
    }

    public int? UbicacionPadreId { get; set; }
    public UbicacionAlmacen? UbicacionPadre { get; set; }
    public ICollection<UbicacionAlmacen> Hijas { get; set; } = new List<UbicacionAlmacen>();

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public TipoUbicacionAlmacen Tipo { get; set; }
    public bool Activa { get; set; } = true;

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
}