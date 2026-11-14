import { PagedRequest } from './api-response.model';

export enum EstadoTransferenciaInventario {
  Borrador = 0,
  Solicitada = 1,
  Aprobada = 2,
  EnTransito = 3,
  Recibida = 4,
  Cancelada = 5
}

export interface TransferenciaInventarioDetalleInput {
  productoVarianteId: number;
  ubicacionOrigenId?: number | null;
  ubicacionDestinoId?: number | null;
  cantidadSolicitada: number;
}

export interface TransferenciaInventarioFormValue {
  almacenOrigenId: number;
  almacenDestinoId: number;
  observaciones?: string | null;
  detalles: TransferenciaInventarioDetalleInput[];
}

export interface AprobarTransferenciaInventarioDetalle {
  detalleId: number;
  cantidadAprobada: number;
}

export interface AprobarTransferenciaInventario {
  detalles: AprobarTransferenciaInventarioDetalle[];
}

export interface DespacharTransferenciaInventarioDetalle {
  detalleId: number;
  cantidadDespachada: number;
}

export interface DespacharTransferenciaInventario {
  detalles: DespacharTransferenciaInventarioDetalle[];
}

export interface RecibirTransferenciaInventarioDetalle {
  detalleId: number;
  cantidadRecibida: number;
  cantidadFaltante: number;
  cantidadDanada: number;
  cantidadSobrante: number;
}

export interface RecibirTransferenciaInventario {
  detalles: RecibirTransferenciaInventarioDetalle[];
}

export interface CancelarTransferenciaInventario {
  motivo: string;
}

export interface TransferenciaInventarioDetalle {
  id: number;
  productoVarianteId: number;
  ubicacionOrigenId?: number | null;
  ubicacionDestinoId?: number | null;
  cantidadSolicitada: number;
  cantidadAprobada: number;
  cantidadDespachada: number;
  cantidadRecibida: number;
  cantidadFaltante: number;
  cantidadSobrante: number;
  cantidadDanada: number;
  productoSkuSnapshot?: string | null;
  productoMarcaSnapshot?: string | null;
  productoModeloSnapshot?: string | null;
  productoColorSnapshot?: string | null;
  productoTallaSnapshot?: string | null;
}

export interface TransferenciaInventario {
  id: number;
  numero: string;
  almacenOrigenId: number;
  almacenOrigenNombre?: string | null;
  almacenDestinoId: number;
  almacenDestinoNombre?: string | null;
  estado: string;
  observaciones?: string | null;
  fechaSolicitud?: string | null;
  fechaAprobacion?: string | null;
  fechaDespacho?: string | null;
  fechaRecepcion?: string | null;
  fechaCancelacion?: string | null;
  motivoCancelacion?: string | null;
  detalles: TransferenciaInventarioDetalle[];
}

export interface TransferenciaInventarioFiltro extends PagedRequest {
  estado?: EstadoTransferenciaInventario;
  almacenOrigenId?: number;
  almacenDestinoId?: number;
  desde?: string;
  hasta?: string;
  numero?: string;
}
