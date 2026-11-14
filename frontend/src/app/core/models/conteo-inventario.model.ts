import { PagedRequest } from './api-response.model';

export enum EstadoConteoInventario {
  Borrador = 1,
  EnProceso = 2,
  Cerrado = 3,
  Aprobado = 4,
  Cancelado = 5
}

export enum TipoConteoInventario {
  General = 1,
  Ciclico = 2,
  PorUbicacion = 3,
  PorCategoria = 4,
  Ciego = 5
}

export interface ConteoInventarioDetalle {
  id: number;
  conteoInventarioId: number;
  productoVarianteId: number;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  stockEsperado?: number | null;
  cantidadContada?: number | null;
  diferencia?: number | null;
  fechaConteo?: string | null;
  contadoPorUsuarioId?: number | null;
  ajusteInventarioId?: number | null;
  productoSku?: string | null;
  productoMarca?: string | null;
  productoModelo?: string | null;
  productoColor?: string | null;
  productoTalla?: string | null;
}

export interface ConteoInventario {
  id: number;
  numero: string;
  tipo: TipoConteoInventario;
  tipoNombre: string;
  estado: EstadoConteoInventario;
  estadoNombre: string;
  almacenId: number;
  almacenNombre?: string | null;
  ubicacionAlmacenId?: number | null;
  ubicacionNombre?: string | null;
  categoriaId?: number | null;
  categoriaNombre?: string | null;
  esCiego: boolean;
  observaciones?: string | null;
  fechaInicio?: string | null;
  iniciadoPorUsuarioId?: number | null;
  fechaCierre?: string | null;
  cerradoPorUsuarioId?: number | null;
  fechaAprobacion?: string | null;
  aprobadoPorUsuarioId?: number | null;
  fechaCancelacion?: string | null;
  canceladoPorUsuarioId?: number | null;
  motivoCancelacion?: string | null;
  cantidadLineas: number;
  cantidadCapturadas: number;
  cantidadConDiferencia: number;
  diferenciaNeta: number;
  detalles: ConteoInventarioDetalle[];
}

export interface ConteoInventarioFormValue {
  tipo: TipoConteoInventario;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  categoriaId?: number | null;
  esCiego: boolean;
  observaciones?: string | null;
  productoVarianteIds: number[];
}

export interface CapturarConteoInventarioLinea {
  detalleId: number;
  cantidadContada: number;
}

export interface ConteoInventarioFiltro extends PagedRequest {
  almacenId?: number;
  ubicacionAlmacenId?: number;
  categoriaId?: number;
  tipo?: TipoConteoInventario;
  estado?: EstadoConteoInventario;
  desde?: string;
  hasta?: string;
}

export interface ConteoInventarioResumen {
  conteoInventarioId: number;
  totalLineas: number;
  capturadas: number;
  pendientes: number;
  conDiferencia: number;
  diferenciaNeta: number;
  puedeCerrar: boolean;
}
