export interface MovimientoFinanciero {
  id: number;
  fecha: string;
  tipo: 'Ingreso' | 'Egreso' | 'Ajuste';
  categoria: string;
  concepto: string;
  descripcion?: string;
  monto: number;
  estado: 'Pendiente' | 'Pagado' | 'Anulado';
  metodoPago?: string;
  esAutomatico: boolean;
  moduloOrigen: string;
  creadoPorNombreUsuario?: string;
  anuladoPorNombreUsuario?: string;
  fechaAnulacion?: string;
  motivoAnulacion?: string;
}

export interface CreateMovimientoManualValue {
  tipo: string;
  categoria: string;
  concepto: string;
  descripcion?: string;
  monto: number;
  metodoPago?: string;
}

export interface RevisionFinanciera {
  id: number;
  fechaDesde: string;
  fechaHasta: string;
  revisadoPorNombreUsuario: string;
  fechaRevision: string;
  estadoRevision: 'Revisado' | 'ConObservaciones';
  observaciones?: string;
}

export interface CreateRevisionValue {
  fechaDesde: string;
  fechaHasta: string;
  estadoRevision: string;
  observaciones?: string;
}

export interface FinanzasResumen {
  ingresosTotales: number;
  egresosTotales: number;
  gastosOperativos: number;
  utilidadBruta: number;
  margenUtilidadBruta: number;
  utilidadNeta: number;
  valorInventarioCosto: number;
  valorInventarioCostoMercaderia: number;
  valorInventarioCostoInsumosAdministrativos: number;
  valorPotencialVenta: number;
  valorPotencialVentaMercaderia: number;
  utilidadInventarioPotencial: number;
  margenInventarioPotencial: number;
  cuentasPorCobrar: number;
  cuentasPorPagar: number;
  balanceOperativo: number;
  ventasDelMes: number;
  comprasDelMes: number;
  ingresosDelMes: number;
  ultimaRevision?: RevisionFinanciera;
}
