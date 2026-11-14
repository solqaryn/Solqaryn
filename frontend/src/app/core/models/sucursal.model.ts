export interface Sucursal {
  id: number;
  empresaId?: number | null;
  codigo: string;
  nombre: string;
  direccion?: string | null;
  telefono?: string | null;
  correo?: string | null;
  zonaHoraria: string;
  activa: boolean;
  creadoPorNombreUsuario?: string | null;
  actualizadoPorNombreUsuario?: string | null;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface SucursalFormValue {
  empresaId: number;
  codigo: string;
  nombre: string;
  direccion?: string | null;
  telefono?: string | null;
  correo?: string | null;
  zonaHoraria: string;
}

export interface SucursalFiltro {
  buscar?: string;
  activa?: boolean;
  empresaId?: number;
  pagina: number;
  tamanoPagina: number;
}

export interface SucursalPagina {
  items: Sucursal[];
  pagina: number;
  tamanoPagina: number;
  total: number;
  totalPaginas: number;
}
