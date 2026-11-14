export enum EstadoPeriodoContable {
  Abierto = 1,
  Cerrado = 2
}

export interface PeriodoContable {
  id: number;
  fechaInicio: string;
  fechaFin: string;
  estado: EstadoPeriodoContable;
  cerradoEnUtc: string | null;
}

export interface CrearPeriodoContableDto {
  fechaInicio: string;
  fechaFin: string;
}

export interface PeriodoContableQuery {
  page: number;
  pageSize: number;
  fechaDesde?: string;
  fechaHasta?: string;
  estado?: EstadoPeriodoContable;
}
