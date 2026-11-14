export type EstadoCuentaPorPagar = 1 | 2 | 3 | 4;
export type CondicionPagoProveedor = 1 | 2;
export type TipoAplicacionCuentaPorPagar = 1 | 2 | 3 | 4;

export interface CuentaPorPagarFiltroDto {
  estado?: EstadoCuentaPorPagar | null;
  condicionPago?: CondicionPagoProveedor | null;
  proveedorId?: number | null;
  facturaProveedorId?: number | null;
  venceDesdeUtc?: string | null;
  venceHastaUtc?: string | null;
  moneda?: string | null;
  sortDirection?: 'asc' | 'desc';
  page: number;
  pageSize: number;
}

export interface AplicacionCuentaPorPagarDto {
  id: number;
  tipo: TipoAplicacionCuentaPorPagar;
  monto: number;
  idempotencyKey: string;
  referenciaExterna?: string | null;
  fechaAplicacionUtc: string;
  revertida: boolean;
  fechaReversionUtc?: string | null;
  motivoReversion?: string | null;
}

export interface CuentaPorPagarDto {
  id: number;
  facturaProveedorId: number;
  proveedorId: number;
  moneda: string;
  condicionPago: CondicionPagoProveedor;
  fechaEmisionUtc: string;
  fechaVencimientoUtc: string;
  montoOriginal: number;
  montoAplicado: number;
  saldo: number;
  estado: EstadoCuentaPorPagar;
  fechaAnulacionUtc?: string | null;
  motivoAnulacion?: string | null;
  aplicaciones: AplicacionCuentaPorPagarDto[];
}

export interface GenerarCuentaPorPagarDto {
  facturaProveedorId: number;
  condicionPago: CondicionPagoProveedor;
  fechaVencimientoUtc?: string | null;
}

export interface AplicarCuentaPorPagarDto {
  tipo: TipoAplicacionCuentaPorPagar;
  monto: number;
  idempotencyKey: string;
  referenciaExterna?: string | null;
  fechaAplicacionUtc: string;
}

export interface RevertirAplicacionCuentaPorPagarDto {
  idempotencyKey: string;
  motivo: string;
  fechaReversionUtc: string;
}

export interface AnularCuentaPorPagarDto {
  motivo: string;
  fechaAnulacionUtc: string;
}
