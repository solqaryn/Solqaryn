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

export interface CategoriaImagen {
  id: string;
  url: string;
}

export interface CategoriaGaleria {
  version: string;
  imagenes: CategoriaImagen[];
}

export interface CategoriaFormValue {
  nombre: string;
  descripcion?: string;
  activa?: boolean;
}
