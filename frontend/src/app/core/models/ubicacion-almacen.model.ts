export interface UbicacionAlmacen {
  id: number;
  almacenId: number;
  almacenCodigo: string;
  almacenNombre: string;
  ubicacionPadreId?: number | null;
  ubicacionPadreCodigo?: string | null;
  ubicacionPadreNombre?: string | null;
  codigo: string;
  nombre: string;
  tipo: string;
  activa: boolean;
  creadoPorNombreUsuario?: string | null;
  actualizadoPorNombreUsuario?: string | null;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface UbicacionAlmacenFormValue {
  almacenId: number;
  ubicacionPadreId?: number | null;
  codigo: string;
  nombre: string;
  tipo: string;
}

export interface TipoUbicacionAlmacenOpcion {
  codigo: string;
  nombre: string;
}

export interface UbicacionAlmacenFiltro {
  buscar?: string;
  almacenId?: number;
  ubicacionPadreId?: number;
  soloRaiz?: boolean;
  tipo?: string;
  activa?: boolean;
  pagina: number;
  tamanoPagina: number;
}

export interface UbicacionAlmacenPagina {
  items: UbicacionAlmacen[];
  pagina: number;
  tamanoPagina: number;
  total: number;
  totalPaginas: number;
}
