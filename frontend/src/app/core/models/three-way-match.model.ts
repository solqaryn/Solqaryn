export enum ThreeWayMatchStatus {
  Pendiente = 0,
  Aprobado = 1,
  Discrepancia = 2
}

export enum ThreeWayMatchDiscrepancyType {
  Cantidad = 1,
  Precio = 2,
  Descuento = 3,
  Impuesto = 4,
  Moneda = 5
}

export interface ThreeWayMatchLineDiscrepancyDto {
  ordenCompraDetalleId: number;
  tipo: ThreeWayMatchDiscrepancyType;
  esperadoOrdenado: number;
  valorRecepcion: number;
  valorFacturado: number;
  mensaje: string;
  esperadoTexto?: string | null;
  valorFacturadoTexto?: string | null;
}

export interface ThreeWayMatchResultDto {
  ordenCompraId: number;
  estado: ThreeWayMatchStatus;
  discrepancias: ThreeWayMatchLineDiscrepancyDto[];
}
