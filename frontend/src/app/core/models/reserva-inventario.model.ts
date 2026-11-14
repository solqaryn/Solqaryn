export type EstadoReservaInventario =
  | 'Borrador'
  | 'Activa'
  | 'Consumida'
  | 'Liberada'
  | 'Expirada'
  | 'Cancelada';

export interface ReservaInventarioDetalle {
  id: number;
  productoVarianteId: number;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  cantidadReservada: number;
  cantidadConsumida: number;
  productoSku?: string | null;
  productoMarca?: string | null;
  productoModelo?: string | null;
  productoColor?: string | null;
  productoTalla?: string | null;
}

export interface ReservaInventario {
  id: number;
  numero: string;
  ventaId?: number | null;
  estado: EstadoReservaInventario | string;
  fechaExpiracion?: string | null;
  fechaCreacion: string;
  fechaActivacion?: string | null;
  fechaConsumo?: string | null;
  fechaLiberacion?: string | null;
  fechaExpiracionAplicada?: string | null;
  fechaCancelacion?: string | null;
  motivoLiberacion?: string | null;
  motivoCancelacion?: string | null;
  detalles: ReservaInventarioDetalle[];
}

export interface ReservaInventarioDetalleInput {
  productoVarianteId: number;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  cantidad: number;
}

export interface ReservaInventarioFormValue {
  ventaId?: number | null;
  fechaExpiracion?: string | null;
  detalles: ReservaInventarioDetalleInput[];
}

export interface ActualizarReservaInventarioValue {
  fechaExpiracion?: string | null;
  detalles: ReservaInventarioDetalleInput[];
}

export interface ReservaInventarioFiltro {
  busqueda?: string;
  estado?: EstadoReservaInventario | string;
  ventaId?: number;
  almacenId?: number;
  expiraDesde?: string;
  expiraHasta?: string;
  page: number;
  pageSize: number;
}
