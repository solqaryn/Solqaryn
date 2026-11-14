export enum TipoCentroCosto {
  Sucursal = 1,
  Departamento = 2,
  Proyecto = 3,
  UnidadNegocio = 4
}

export interface CentroCosto {
  id: number;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  tipo: TipoCentroCosto;
  sucursalId?: number | null;
  sucursalCodigo?: string | null;
  sucursalNombre?: string | null;
  activo: boolean;
  eliminado: boolean;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface CreateCentroCostoDto {
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  tipo: TipoCentroCosto;
  sucursalId?: number | null;
}

export interface UpdateCentroCostoDto extends CreateCentroCostoDto {
  activo: boolean;
}

export interface CentroCostoFiltro {
  termino?: string;
  tipo?: TipoCentroCosto;
  sucursalId?: number;
  activo?: boolean;
  pagina: number;
  tamanoPagina: number;
}

export interface CentroCostoPagina {
  items: CentroCosto[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}
