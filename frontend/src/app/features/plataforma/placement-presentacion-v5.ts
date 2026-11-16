import { InjectionToken, signal, type Signal } from '@angular/core';

export type MarcaPlacementV5 = 'solqaryn' | 'institucional';
export type DensidadPlacementV5 = 'normal' | 'compacta';
export interface PresentacionPlacementV5 {
  readonly marca: MarcaPlacementV5;
  readonly densidad: DensidadPlacementV5;
}
const VALOR_SEGURO: PresentacionPlacementV5 = Object.freeze({
  marca: 'solqaryn', densidad: 'normal'
});

/** Exclusivamente presentacional: nunca concede permisos, tenant ni routing MySQL. */
export const PLACEMENT_PRESENTACION_V5 = new InjectionToken<Signal<unknown>>(
  'PLACEMENT_PRESENTACION_V5',
  { providedIn: 'root', factory: () => signal<unknown>(VALOR_SEGURO) }
);

export function normalizarPresentacionPlacementV5(valor: unknown): PresentacionPlacementV5 {
  if (valor === null || typeof valor !== 'object' || Array.isArray(valor)) return VALOR_SEGURO;
  const campos = valor as Record<string, unknown>;
  if (Object.keys(campos).length !== 2 ||
      !Object.prototype.hasOwnProperty.call(campos, 'marca') ||
      !Object.prototype.hasOwnProperty.call(campos, 'densidad')) return VALOR_SEGURO;
  const marca = campos['marca'];
  const densidad = campos['densidad'];
  if ((marca !== 'solqaryn' && marca !== 'institucional') ||
      (densidad !== 'normal' && densidad !== 'compacta')) return VALOR_SEGURO;
  return Object.freeze({ marca, densidad });
}
