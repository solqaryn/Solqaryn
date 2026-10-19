export interface PermisoMatrizItem {
  rol: string;
  modulo: string;
  accion: string;
  permitido: boolean;
}

export interface MisPermisos {
  rol: string;
  esAdministrador: boolean;
  permisos: string[]; // formato "Modulo:Accion"
  operacionMultiEmpresaRestringida: boolean;
  capacidadesHabilitadas: string[]; // proyección UI del backend, no autoridad
  rutasHabilitadas: string[]; // patrones exactos de Angular certificados
}

export interface PermisoCatalogo {
  id: number;
  codigo: string;
  nombre: string;
  descripcion?: string;
  modulo: string;
  accion: string;
  esSistema: boolean;
  activo: boolean;
  cantidadRolesAsignados: number;
  fechaCreacion: string;
  fechaActualizacion?: string;
}

export interface CrearPermisoValue {
  nombre: string;
  descripcion?: string;
  modulo: string;
  accion: string;
}
