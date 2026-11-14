export interface Almacen {
  id: number;
  sucursalId: number;
  sucursalCodigo: string;
  sucursalNombre: string;
  codigo: string;
  nombre: string;
  tipo: string;
  activo: boolean;
  creadoPorNombreUsuario?: string | null;
  actualizadoPorNombreUsuario?: string | null;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface AlmacenFormValue {
  sucursalId: number;
  codigo: string;
  nombre: string;
  tipo: string;
}

export interface TipoAlmacenOpcion {
  codigo: string;
  nombre: string;
}

export interface AlmacenFiltro {
  buscar?: string;
  activo?: boolean;
  sucursalId?: number;
  tipo?: string;
  pagina: number;
  tamanoPagina: number;
}

export interface AlmacenPagina {
  items: Almacen[];
  pagina: number;
  tamanoPagina: number;
  total: number;
  totalPaginas: number;
}
