namespace Solqaryn.Application.DTOs;

public class ReporteAdministrativoFiltroDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}

public class ResumenAdministrativoDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public int UsuariosTotales { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsuariosBloqueados { get; set; }
    public int UsuariosEliminados { get; set; }
    public int UsuariosPrivilegiados { get; set; }
    public int RolesTotales { get; set; }
    public int RolesActivos { get; set; }
    public int RolesSinPermisos { get; set; }
    public int RolesSinUsuarios { get; set; }
    public int PermisosCatalogados { get; set; }
    public int EventosAuditoria { get; set; }
    public int EventosExitosos { get; set; }
    public int EventosRechazados { get; set; }
    public int EventosConError { get; set; }
    public List<ActividadAuditoriaResumenDto> ActividadPorModulo { get; set; } = new();
    public List<AlertaAdministrativaDto> Alertas { get; set; } = new();
}

public class ActividadAuditoriaResumenDto
{
    public string Modulo { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Exitosos { get; set; }
    public int Rechazados { get; set; }
    public int ConError { get; set; }
}

public class AlertaAdministrativaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Severidad { get; set; } = "Informativa";
    public string Mensaje { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class UsuarioAccesoReporteDto
{
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public int? RolId { get; set; }
    public string Rol { get; set; } = "Sin rol";
    public bool EsAdministrador { get; set; }
    public bool RolActivo { get; set; }
    public bool Activo { get; set; }
    public bool Bloqueado { get; set; }
    public bool Eliminado { get; set; }
    public int PermisosEfectivos { get; set; }
    public int PermisosSensibles { get; set; }
    public string EstadoAcceso { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

public class RolPermisosReporteDto
{
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool EsSistema { get; set; }
    public bool EsAdministrador { get; set; }
    public bool Activo { get; set; }
    public bool Eliminado { get; set; }
    public int UsuariosAsignados { get; set; }
    public int PermisosAsignados { get; set; }
    public int ModulosConAcceso { get; set; }
    public int PermisosSensibles { get; set; }
    public decimal PorcentajeCobertura { get; set; }
    public string NivelPrivilegio { get; set; } = string.Empty;
    public string EstadoConfiguracion { get; set; } = string.Empty;
    public List<string> Permisos { get; set; } = new();
}

public class AuditoriaResumenDto
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public int Total { get; set; }
    public int Exitosos { get; set; }
    public int Rechazados { get; set; }
    public int ConError { get; set; }
    public int UsuariosUnicos { get; set; }
    public List<ActividadAuditoriaResumenDto> PorModulo { get; set; } = new();
    public List<ActividadAuditoriaAccionDto> PorAccion { get; set; } = new();
}

public class ActividadAuditoriaAccionDto
{
    public string Accion { get; set; } = string.Empty;
    public int Total { get; set; }
}
