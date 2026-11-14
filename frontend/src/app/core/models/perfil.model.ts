export interface Perfil {
  id: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: string;
  fotoPerfilUrl?: string;
  fechaCreacion: string;
}

export interface ActualizarPerfilValue {
  nombreUsuario: string;
  nombreCompleto: string;
}

export interface CambiarPasswordValue {
  passwordActual: string;
  passwordNueva: string;
}
