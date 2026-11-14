export enum EstadoPedidoVenta { Borrador=1, Confirmado=2, Anulado=3 }
export interface PedidoVentaDetalle { id:number; productoId:number; productoVarianteId?:number|null; productoSkuSnapshot?:string|null; productoNombreSnapshot?:string|null; productoMarcaSnapshot?:string|null; productoModeloSnapshot?:string|null; productoColorSnapshot?:string|null; productoTallaSnapshot?:string|null; cantidad:number; precioUnitario:number; total:number; }
export interface PedidoVenta { id:number; cotizacionId?:number|null; clienteId:number; clienteNombreSnapshot:string; clienteDocumentoSnapshot?:string|null; observaciones?:string|null; estado:EstadoPedidoVenta; total:number; fechaConfirmacionUtc?:string|null; confirmadoPorUsuarioId?:number|null; fechaAnulacionUtc?:string|null; anuladoPorUsuarioId?:number|null; motivoAnulacion?:string|null; detalles:PedidoVentaDetalle[]; }
export interface PedidoVentaFiltro { cotizacionId?:number|null; clienteId?:number|null; estado?:EstadoPedidoVenta|null; fechaDesdeUtc?:string|null; fechaHastaUtc?:string|null; page:number; pageSize:number; sortBy?:string; sortDirection?:'asc'|'desc'; }
export interface CreatePedidoVenta { cotizacionId:number; observaciones?:string|null; }
export interface UpdatePedidoVenta { id:number; observaciones?:string|null; }
export interface AsignacionReservaPedido { productoVarianteId:number; almacenId:number; ubicacionAlmacenId?:number|null; cantidad:number; }
export interface ConfirmarPedidoVenta { asignaciones:AsignacionReservaPedido[]; }
