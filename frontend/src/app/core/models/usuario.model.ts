export interface Usuario {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: string;
  rolId?: number;
  activo: boolean;
  bloqueado: boolean;
  fechaCreacion: string;
}

export interface UsuarioDetalle {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: string;
  rolId?: number;
  rolNombre?: string;
  activo: boolean;
  bloqueado: boolean;
  motivoBloqueo?: string;
  fechaBloqueo?: string;
  fechaCreacion: string;
  fechaActualizacion?: string;
}

export interface CreateUsuarioValue {
  nombreUsuario: string;
  nombreCompleto: string;
  password: string;
  rol: string;
  rolId?: number;
}

export interface UpdateUsuarioValue {
  nombreUsuario: string;
  nombreCompleto: string;
  rol: string;
  rolId?: number;
  nuevaPassword?: string;
}
