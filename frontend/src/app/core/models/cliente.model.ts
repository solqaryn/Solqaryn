export interface Cliente {
  id: number;
  nombre: string;
  telefono?: string;
  identidadORTN?: string;
  correo?: string;
  direccion?: string;
  activo: boolean;
  totalVentas: number;
  totalVendido: number;
  tipoClienteId: number;
  tipoClienteNombre: string;
  tipoClienteColorHex: string;
  creadoPorNombreUsuario?: string;
  fechaCreacion: string;
}

export interface ClienteFormValue {
  nombre: string;
  telefono?: string;
  identidadORTN?: string;
  correo?: string;
  direccion?: string;
  tipoClienteId?: number;
  activo?: boolean;
}
