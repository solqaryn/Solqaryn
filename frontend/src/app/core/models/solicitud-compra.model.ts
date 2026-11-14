export type EstadoSolicitudCompra = 'Borrador' | 'Solicitada' | 'Aprobada' | 'Rechazada';

export interface SolicitudCompraDetalleInput {
  productoId: number;
  productoVarianteId?: number | null;
  cantidadSolicitada: number;
  costoEstimadoUnitario?: number | null;
  observacion?: string | null;
}

export interface SolicitudCompraFormValue {
  proveedorId?: number | null;
  notas?: string | null;
  detalles: SolicitudCompraDetalleInput[];
}

export interface SolicitudCompraFiltro {
  page: number;
  pageSize: number;
  estado?: EstadoSolicitudCompra | null;
  proveedorId?: number | null;
  desde?: string | null;
  hasta?: string | null;
  numero?: string | null;
}

export interface SolicitudCompraDetalle {
  id: number;
  productoId: number;
  productoVarianteId?: number | null;
  cantidadSolicitada: number;
  costoEstimadoUnitario?: number | null;
  observacion?: string | null;
  productoSkuSnapshot?: string | null;
  productoNombreSnapshot?: string | null;
  productoMarcaSnapshot?: string | null;
  productoModeloSnapshot?: string | null;
  productoColorSnapshot?: string | null;
  productoTallaSnapshot?: string | null;
}

export interface SolicitudCompra {
  id: number;
  numeroSolicitud: string;
  estado: EstadoSolicitudCompra;
  proveedorId?: number | null;
  proveedorNombre?: string | null;
  notas?: string | null;
  fechaSolicitudUtc?: string | null;
  solicitadaPorUsuarioId?: number | null;
  solicitadaPorNombreSnapshot?: string | null;
  fechaDecisionUtc?: string | null;
  decididaPorUsuarioId?: number | null;
  decididaPorNombreSnapshot?: string | null;
  motivoRechazo?: string | null;
  detalles: SolicitudCompraDetalle[];
}
