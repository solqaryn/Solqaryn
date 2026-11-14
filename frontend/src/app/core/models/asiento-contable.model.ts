export interface CrearAsientoDetalleDto {
  cuentaContableId: number;
  debe: number;
  haber: number;
  referencia?: string;
}

export interface CrearAsientoContableDto {
  fecha?: string;
  concepto: string;
  numero?: string;
  documentoOrigenId?: number;
  tipoDocumentoOrigen?: string;
  detalles: CrearAsientoDetalleDto[];
}

export interface AsientoDetalleDto {
  id: number;
  cuentaContableId: number;
  cuentaCodigo?: string;
  cuentaNombre?: string;
  debe: number;
  haber: number;
  referencia?: string;
}

export interface AsientoContableDto {
  id: number;
  fecha: string;
  concepto: string;
  numero?: string;
  documentoOrigenId?: number;
  tipoDocumentoOrigen?: string;
  totalDebe: number;
  totalHaber: number;
  detalles: AsientoDetalleDto[];
}
