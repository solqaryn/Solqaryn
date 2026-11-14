export interface ExistenciaVariante {
  id: number;
  productoVarianteId: number;
  productoNombre: string;
  varianteSku: string;
  almacenId: number;
  almacenCodigo: string;
  almacenNombre: string;
  ubicacionAlmacenId?: number | null;
  ubicacionCodigo?: string | null;
  ubicacionNombre?: string | null;
  stockFisico: number;
  stockReservado: number;
  stockDisponible: number;
  stockTransito: number;
  stockMinimo: number;
  stockMaximo?: number | null;
  tieneStockBajo: boolean;
  estaAgotada: boolean;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface ExistenciaVarianteFiltro {
  page: number;
  pageSize: number;
  productoId?: number;
  productoVarianteId?: number;
  almacenId?: number;
  ubicacionAlmacenId?: number;
  soloRaizAlmacen?: boolean;
  stockBajo?: boolean;
  agotada?: boolean;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface CreateExistenciaVariante {
  productoVarianteId: number;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  stockFisico: number;
  stockReservado: number;
  stockTransito: number;
  stockMinimo: number;
  stockMaximo?: number | null;
}

export interface UpdateExistenciaVarianteConfiguracion {
  ubicacionAlmacenId?: number | null;
  stockMinimo: number;
  stockMaximo?: number | null;
}
