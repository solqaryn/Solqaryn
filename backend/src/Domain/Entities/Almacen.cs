using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Punto físico/lógico de almacenamiento asociado a una Sucursal.
/// N1.2 define únicamente la topología maestra; existencias y ubicaciones
/// internas se incorporan en ERP-N1.3/N1.4.
/// El ownership de Empresa se deriva exclusivamente de Sucursal para evitar
/// una segunda ruta persistida que pueda contradecir la jerarquía tenant-aware.
/// </summary>
public class Almacen : AuditableEntity
{
    public int SucursalId { get; set; }
    public Sucursal Sucursal { get; set; } = null!;

    /// <summary>
    /// Resuelve la Empresa propietaria a través de la Sucursal canónica.
    /// Falla de forma explícita si el agregado no dispone de la jerarquía necesaria.
    /// </summary>
    public int ObtenerEmpresaIdTenant()
    {
        if (Sucursal is null)
        {
            throw new InvalidOperationException("El almacén no tiene una Sucursal cargada para resolver su Empresa tenant.");
        }

        return Sucursal.ObtenerEmpresaIdTenant();
    }

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public TipoAlmacen Tipo { get; set; }
    public bool Activo { get; set; } = true;

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
}