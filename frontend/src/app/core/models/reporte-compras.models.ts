import { EstadoOrdenCompra } from './orden-compra.model';

export type EstadoFacturaProveedorNombre = 'Borrador' | 'Registrada' | 'Anulada';
export type EstadoFacturaProveedor = EstadoFacturaProveedorNombre | 1 | 2 | 3;

export type EstadoRecepcionCompraNombre = 'Borrador' | 'Recibida' | 'Anulada';
export type EstadoRecepcionCompra = EstadoRecepcionCompraNombre | 1 | 2 | 3;

export type EstadoDevolucionProveedorNombre = 'Borrador' | 'Confirmada' | 'Anulada';
export type EstadoDevolucionProveedor = EstadoDevolucionProveedorNombre | 1 | 2 | 3;

export interface ReporteComprasFiltroDto {
  desdeUtc?: string | null;
  hastaUtc?: string | null;
  proveedorId?: number | null;
  productoId?: number | null;
  productoVarianteId?: number | null;
  estadoOrden?: EstadoOrdenCompra | null;
  estadoFactura?: EstadoFacturaProveedor | null;
  estadoRecepcion?: EstadoRecepcionCompra | null;
  estadoDevolucion?: EstadoDevolucionProveedor | null;
  page: number;
  pageSize: number;
}

export interface ReporteComprasDetalleDto {
  ordenCompraId: number;
  ordenCompraDetalleId: number;
  numeroOrden: string;
  fechaCreacionUtc: string;
  fechaEsperadaUtc?: string | null;
  proveedorId: number;
  proveedorNombre: string;
  moneda: string;
  productoId: number;
  productoVarianteId?: number | null;
  productoSku?: string | null;
  productoNombre?: string | null;
  productoMarca?: string | null;
  productoModelo?: string | null;
  productoColor?: string | null;
  productoTalla?: string | null;
  cantidadOrdenada: number;
  precioUnitarioOrdenado: number;
  precioUnitarioFacturado?: number | null;
  monedaFactura?: string | null;
  variacionPrecioAbsoluta?: number | null;
  cantidadRecibida: number;
  cantidadAceptada: number;
  cantidadDanada: number;
  cantidadFaltante: number;
  cantidadSobrante: number;
  cantidadDevueltaEfectiva: number;
  fechaEsperadaEvaluadaUtc?: string | null;
  fechaRecepcionEvaluadaUtc?: string | null;
  desviacionEntregaDias?: number | null;
  cantidadEvaluadaOrdenada?: number | null;
  cantidadEvaluadaAceptada?: number | null;
  cantidadEvaluadaDanada?: number | null;
  cantidadEvaluadaSobrante?: number | null;
}
