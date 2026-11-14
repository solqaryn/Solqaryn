export interface SuscripcionSaaS {
  id: number;
  planCodigo: string;
  planNombre: string;
  estado: number | string;
  inicioUtc: string;
  finUtc?: string | null;
}

export interface LimiteSuscripcionSaaS {
  clave: string;
  valorMaximo?: number | null;
}

export interface EntitlementModuloSaaS {
  moduloClave: string;
  habilitado: boolean;
  motivo: number | string;
  planId?: number | null;
  planCodigo?: string | null;
}

export interface PaginaSuscripcionSaaS<T> {
  items: T[];
  pagina: number;
  tamanoPagina: number;
  total: number;
}

export interface OnboardingSuscripcionSaaS {
  planCodigo: string;
  inicioUtc: string;
}
