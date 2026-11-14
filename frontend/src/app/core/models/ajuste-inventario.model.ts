import { PagedRequest } from './api-response.model';

export type EstadoAjusteInventario = 'Borrador' | 'Confirmado' | 'Anulado';

export interface AjusteInventarioDetalleInput {
  productoId: number;
  productoVarianteId: number;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  cantidadObjetivo: number;
}

export interface AjusteInventarioFormValue {
  fechaAjuste?: string | null;
  motivo: string;
  observaciones?: string | null;
  detalles: AjusteInventarioDetalleInput[];
}

export interface AjusteInventarioDetalle {
  id: number;
  productoId: number;
  productoVarianteId?: number | null;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  cantidadObjetivo: number;
  cantidadAnteriorSnapshot?: number | null;
  cantidadNuevaSnapshot?: number | null;
  diferenciaSnapshot?: number | null;
  costoUnitarioSnapshot?: number | null;
  impactoCostoSnapshot?: number | null;
  nombreSnapshot?: string | null;
  skuSnapshot?: string | null;
  marcaSnapshot?: string | null;
  modeloSnapshot?: string | null;
  colorSnapshot?: string | null;
  tallaSnapshot?: string | null;
}

export interface AjusteInventario {
  id: number;
  numeroAjuste: string;
  fechaAjuste: string;
  estado: EstadoAjusteInventario;
  motivo: string;
  observaciones?: string | null;
  fechaConfirmacion?: string | null;
  confirmadoPorNombreUsuario?: string | null;
  fechaAnulacion?: string | null;
  anuladoPorNombreUsuario?: string | null;
  motivoAnulacion?: string | null;
  impactoCostoTotalSnapshot?: number | null;
  detalles: AjusteInventarioDetalle[];
}

export interface AjusteInventarioFiltro extends PagedRequest {
  estado?: EstadoAjusteInventario;
  desde?: string;
  hasta?: string;
  productoId?: number;
  productoVarianteId?: number;
  almacenId?: number;
  ubicacionAlmacenId?: number;
}
