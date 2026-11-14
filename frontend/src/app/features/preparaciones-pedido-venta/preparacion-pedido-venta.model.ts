export enum EstadoPreparacionPedidoVenta {
  PendientePicking = 1,
  PickingCompletado = 2,
  PackingCompletado = 3,
  Despachado = 4,
  Entregado = 5,
  Cancelado = 6
}

export interface PreparacionPedidoVentaDetalle {
  id: number;
  productoVarianteId: number;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  cantidadPreparar: number;
  productoSkuSnapshot?: string | null;
  productoMarcaSnapshot?: string | null;
  productoModeloSnapshot?: string | null;
  productoColorSnapshot?: string | null;
  productoTallaSnapshot?: string | null;
}

export interface PreparacionPedidoVenta {
  id: number;
  pedidoVentaId: number;
  reservaInventarioId: number;
  estado: EstadoPreparacionPedidoVenta;
  fechaPickingCompletadoUtc?: string | null;
  fechaPackingCompletadoUtc?: string | null;
  fechaDespachoUtc?: string | null;
  fechaEntregaUtc?: string | null;
  fechaCancelacionUtc?: string | null;
  motivoCancelacion?: string | null;
  detalles: PreparacionPedidoVentaDetalle[];
}
