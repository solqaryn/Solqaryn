export interface LoginRequest {
  nombreUsuario: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: string;
  fotoPerfilUrl?: string;
  expiraEn: string;
}
