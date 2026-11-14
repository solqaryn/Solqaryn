export interface Proveedor {
  id: number;
  nombre: string;
  telefono?: string;
  documento?: string;
  correo?: string;
  direccion?: string;
  activo: boolean;
  totalCompras: number;
  totalComprado: number;
  creadoPorNombreUsuario?: string;
  fechaCreacion: string;
}

export interface ProveedorFormValue {
  nombre: string;
  telefono?: string;
  documento?: string;
  correo?: string;
  direccion?: string;
  activo?: boolean;
}
