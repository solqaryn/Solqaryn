export type EstadoRecepcionCompraNombre = 'Borrador' | 'Recibida' | 'Anulada';
export type EstadoRecepcionCompra = EstadoRecepcionCompraNombre | 1 | 2 | 3;

export interface RecepcionCompraFiltro {
  ordenCompraId?: number | null;
  estado?: EstadoRecepcionCompraNombre | null;
  desdeUtc?: string | null;
  hastaUtc?: string | null;
  page: number;
  pageSize: number;
}

export interface RecepcionCompraDetalleInput {
  ordenCompraDetalleId: number;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  cantidadRecibida: number;
  cantidadDanada: number;
  cantidadFaltante: number;
  cantidadSobrante: number;
}

export interface RecepcionCompraFormValue {
  ordenCompraId: number;
  observaciones?: string | null;
  detalles: RecepcionCompraDetalleInput[];
}

export interface RecepcionCompraDetalle {
  id: number;
  ordenCompraDetalleId: number;
  productoId: number;
  productoVarianteId?: number | null;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  cantidadRecibida: number;
  cantidadAceptada: number;
  cantidadDanada: number;
  cantidadFaltante: number;
  cantidadSobrante: number;
  costoUnitarioSnapshot: number;
  productoSkuSnapshot?: string | null;
  productoNombreSnapshot?: string | null;
  productoMarcaSnapshot?: string | null;
  productoModeloSnapshot?: string | null;
  productoColorSnapshot?: string | null;
  productoTallaSnapshot?: string | null;
}

export interface RecepcionCompra {
  id: number;
  numeroRecepcion: string;
  ordenCompraId: number;
  numeroOrdenCompra?: string | null;
  estado: EstadoRecepcionCompra;
  observaciones?: string | null;
  fechaRecepcionUtc?: string | null;
  recibidaPorUsuarioId?: number | null;
  recibidaPorNombreSnapshot?: string | null;
  fechaAnulacionUtc?: string | null;
  anuladaPorUsuarioId?: number | null;
  motivoAnulacion?: string | null;
  cantidadRecibidaTotal: number;
  cantidadAceptadaTotal: number;
  cantidadDanadaTotal: number;
  cantidadFaltanteTotal: number;
  cantidadSobranteTotal: number;
  detalles: RecepcionCompraDetalle[];
}

export interface RecepcionCompraSaldoLinea {
  ordenCompraDetalleId: number;
  productoId: number;
  productoVarianteId?: number | null;
  productoSkuSnapshot?: string | null;
  productoNombreSnapshot?: string | null;
  cantidadOrdenada: number;
  cantidadAceptadaAcumulada: number;
  cantidadPendiente: number;
}

export interface RecepcionCompraSaldoOrden {
  ordenCompraId: number;
  numeroOrden: string;
  estadoOrden: string | number;
  lineas: RecepcionCompraSaldoLinea[];
  completa: boolean;
}
