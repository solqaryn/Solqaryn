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
