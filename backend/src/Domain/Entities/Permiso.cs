using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// Catálogo real de permisos (módulo + acción), almacenado en base de datos.
/// Es la fuente de verdad de qué combinaciones módulo/acción son válidas y
/// asignables; RolPermiso.PermisoId referencia estas filas.
public class Permiso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty; // ej. "ROLES.CREAR"
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ModuloSistema Modulo { get; set; }
    public AccionPermiso Accion { get; set; }

    /// <summary>
    /// Define si el permiso pertenece al plano empresarial o al plano global
    /// de plataforma. Los permisos existentes se mantienen como Empresa.
    /// </summary>
    public AmbitoAutorizacion Ambito { get; set; } = AmbitoAutorizacion.Empresa;

    public bool EsSistema { get; set; }
    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public ICollection<RolPermiso> Asignaciones { get; set; } = new List<RolPermiso>();
}
