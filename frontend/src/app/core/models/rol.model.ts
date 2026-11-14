export interface Rol {
  id: number;
  nombre: string;
  descripcion?: string;
  esSistema: boolean;
  esAdministrador: boolean;
  activo: boolean;
  cantidadUsuarios: number;
  cantidadPermisos: number;
  fechaCreacion: string;
  fechaActualizacion?: string;
}

export interface CrearRolValue {
  nombre: string;
  descripcion?: string;
  esAdministrador: boolean;
}

export interface ActualizarRolValue {
  nombre: string;
  descripcion?: string;
}
