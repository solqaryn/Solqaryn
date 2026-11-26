import { MetodoCosteoInventario } from './costeo-inventario.model';

// El motivo histórico exacto se conserva en persistencia y auditoría. Esta función sólo
// adapta su presentación; no transforma motivos comerciales ni cambia la política.
const MOTIVO_MIGRACION_PROMEDIO = 'Cutover ERP-N1.10 — Promedio Ponderado compatible';

export function presentarMotivoCosteo(motivo: string | null | undefined, metodo: MetodoCosteoInventario): string {
  if (metodo === MetodoCosteoInventario.PromedioPonderado && motivo === MOTIVO_MIGRACION_PROMEDIO) {
    return 'Se adoptó el promedio ponderado como política inicial de valoración de existencias.';
  }
  return motivo?.trim() ? motivo : 'Sin motivo registrado.';
}

/** Explicación comercial del método configurado, sin alterar su cálculo o vigencia. */
export function describirMetodoCosteo(metodo: MetodoCosteoInventario | null | undefined): string | null {
  switch (metodo) {
    case MetodoCosteoInventario.PromedioPonderado:
      return 'El promedio ponderado considera el costo de las existencias y el de las nuevas entradas para calcular un costo unitario promedio.';
    case MetodoCosteoInventario.FIFO:
      return 'El método de primeras entradas, primeras salidas (FIFO) asigna a las salidas el costo de las entradas más antiguas disponibles.';
    case MetodoCosteoInventario.Estandar:
      return 'El costeo estándar utiliza un costo de referencia establecido para valorar las existencias y comparar las diferencias frente al costo real.';
    default:
      return null;
  }
}
