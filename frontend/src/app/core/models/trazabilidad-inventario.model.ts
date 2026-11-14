export interface ConfiguracionTrazabilidadVariante {
  productoVarianteId: number;
  controlaLote: boolean;
  controlaNumeroSerie: boolean;
  controlaFechaVencimiento: boolean;
  diasAlertaVencimiento?: number | null;
}

export interface ConfigurarTrazabilidadVarianteRequest {
  controlaLote: boolean;
  controlaNumeroSerie: boolean;
  controlaFechaVencimiento: boolean;
  diasAlertaVencimiento?: number | null;
}

export interface LoteInventario {
  id: number;
  productoVarianteId: number;
  codigo: string;
  fechaFabricacion?: string | null;
  fechaVencimiento?: string | null;
  activo: boolean;
}

export interface CrearLoteInventarioRequest {
  productoVarianteId: number;
  codigo: string;
  fechaFabricacion?: string | null;
  fechaVencimiento?: string | null;
}

export interface SerieInventario {
  id: number;
  productoVarianteId: number;
  loteInventarioId?: number | null;
  numeroSerie: string;
  estado: number;
}

export interface CrearSerieInventarioRequest {
  productoVarianteId: number;
  loteInventarioId?: number | null;
  numeroSerie: string;
}
