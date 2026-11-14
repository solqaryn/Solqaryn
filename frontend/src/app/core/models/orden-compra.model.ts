export type EstadoOrdenCompraNombre = 'Borrador' | 'PendienteAprobacion' | 'Aprobada' | 'Cancelada';
export type EstadoOrdenCompra = EstadoOrdenCompraNombre | 1 | 2 | 3 | 4;

export interface OrdenCompraDetalleInput {
  productoId: number;
  productoVarianteId?: number | null;
  cantidadOrdenada: number;
  precioUnitario: number;
  descuento: number;
  impuesto: number;
  observacion?: string | null;
}

export interface OrdenCompraFormValue {
  solicitudCompraId?: number | null;
  proveedorId: number;
  moneda: string;
  condicionesCompra?: string | null;
  fechaEsperadaUtc?: string | null;
  observaciones?: string | null;
  detalles: OrdenCompraDetalleInput[];
}

export interface OrdenCompraFiltro {
  page: number;
  pageSize: number;
  estado?: EstadoOrdenCompraNombre | null;
  proveedorId?: number | null;
  solicitudCompraId?: number | null;
  numero?: string | null;
  desde?: string | null;
  hasta?: string | null;
  search?: string | null;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc' | null;
}

export interface OrdenCompraDetalle {
  id: number;
  productoId: number;
  productoVarianteId?: number | null;
  cantidadOrdenada: number;
  precioUnitario: number;
  descuento: number;
  impuesto: number;
  subtotal: number;
  total: number;
  observacion?: string | null;
  productoSkuSnapshot?: string | null;
  productoNombreSnapshot?: string | null;
  productoMarcaSnapshot?: string | null;
  productoModeloSnapshot?: string | null;
  productoColorSnapshot?: string | null;
  productoTallaSnapshot?: string | null;
}

export interface OrdenCompra {
  id: number;
  numeroOrden: string;
  estado: EstadoOrdenCompra;
  solicitudCompraId?: number | null;
  proveedorId: number;
  proveedorNombre: string;
  moneda: string;
  condicionesCompra?: string | null;
  fechaEsperadaUtc?: string | null;
  observaciones?: string | null;
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
  fechaEnvioAprobacionUtc?: string | null;
  fechaAprobacionUtc?: string | null;
  fechaCancelacionUtc?: string | null;
  detalles: OrdenCompraDetalle[];
}
