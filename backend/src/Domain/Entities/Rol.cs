namespace Solqaryn.Domain.Entities;

/// Catálogo dinámico de roles (reemplaza el enum estático RolUsuario como fuente
/// de verdad). RolUsuario se conserva únicamente para compatibilidad de JWTs
/// existentes durante la migración; Rol.Id es la referencia real desde Usuario.
public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombreNormalizado { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsSistema { get; set; }
    public bool EsAdministrador { get; set; }

    /// <summary>
    /// Delimita la autoridad del rol. Empresa mantiene el RBAC tenant actual;
    /// Plataforma queda reservado para autoridad global del SaaS.
    /// </summary>
    public Solqaryn.Domain.Enums.AmbitoAutorizacion Ambito { get; set; } =
        Solqaryn.Domain.Enums.AmbitoAutorizacion.Empresa;

    /// <summary>
    /// Propietario opcional de un rol empresarial personalizado.
    /// Null + Ambito Empresa representa un rol empresarial de sistema/plantilla.
    /// Debe permanecer null para roles de Plataforma.
    /// </summary>
    public int? EmpresaId { get; set; }

    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<RolPermiso> Permisos { get; set; } = new List<RolPermiso>();
}
