export interface Categoria {
  id: number;
  nombre: string;
  descripcion?: string;
  activa: boolean;
  totalProductos: number;
  creadoPorNombreUsuario?: string;
  actualizadoPorNombreUsuario?: string;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface CategoriaFormValue {
  nombre: string;
  descripcion?: string;
  activa?: boolean;
}
