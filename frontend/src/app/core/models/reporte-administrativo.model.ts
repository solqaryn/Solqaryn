export interface ActividadAuditoriaResumen {
  modulo: string;
  total: number;
  exitosos: number;
  rechazados: number;
  conError: number;
}

export interface ActividadAuditoriaAccion {
  accion: string;
  total: number;
}

export interface AlertaAdministrativa {
  codigo: string;
  severidad: string;
  mensaje: string;
  cantidad: number;
}

export interface ResumenAdministrativo {
  desde: string;
  hasta: string;
  usuariosTotales: number;
  usuariosActivos: number;
  usuariosBloqueados: number;
  usuariosEliminados: number;
  usuariosPrivilegiados: number;
  rolesTotales: number;
  rolesActivos: number;
  rolesSinPermisos: number;
  rolesSinUsuarios: number;
  permisosCatalogados: number;
  eventosAuditoria: number;
  eventosExitosos: number;
  eventosRechazados: number;
  eventosConError: number;
  actividadPorModulo: ActividadAuditoriaResumen[];
  alertas: AlertaAdministrativa[];
}

export interface UsuarioAccesoReporte {
  usuarioId: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rolId?: number;
  rol: string;
  esAdministrador: boolean;
  rolActivo: boolean;
  activo: boolean;
  bloqueado: boolean;
  eliminado: boolean;
  permisosEfectivos: number;
  permisosSensibles: number;
  estadoAcceso: string;
  fechaCreacion: string;
  fechaActualizacion?: string;
}

export interface RolPermisosReporte {
  rolId: number;
  rol: string;
  esSistema: boolean;
  esAdministrador: boolean;
  activo: boolean;
  eliminado: boolean;
  usuariosAsignados: number;
  permisosAsignados: number;
  modulosConAcceso: number;
  permisosSensibles: number;
  porcentajeCobertura: number;
  nivelPrivilegio: string;
  estadoConfiguracion: string;
  permisos: string[];
}

export interface AuditoriaResumen {
  desde: string;
  hasta: string;
  total: number;
  exitosos: number;
  rechazados: number;
  conError: number;
  usuariosUnicos: number;
  porModulo: ActividadAuditoriaResumen[];
  porAccion: ActividadAuditoriaAccion[];
}
