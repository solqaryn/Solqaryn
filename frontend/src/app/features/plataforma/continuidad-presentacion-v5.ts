import { InjectionToken, signal, type Signal } from '@angular/core';

export type MarcaContinuidadV5 = 'solqaryn' | 'institucional';
export type DensidadContinuidadV5 = 'normal' | 'compacta';
export interface PresentacionContinuidadV5 {
  readonly marca: MarcaContinuidadV5;
  readonly densidad: DensidadContinuidadV5;
}

const SEGURA: PresentacionContinuidadV5 = Object.freeze({ marca: 'solqaryn', densidad: 'normal' });

/** Configuracion reactiva PRESENTACIONAL, nunca autoridad RBAC/JWT ni entrada de tenant. */
export const CONTINUIDAD_PRESENTACION_V5 = new InjectionToken<Signal<unknown>>(
  'CONTINUIDAD_PRESENTACION_V5',
  { providedIn: 'root', factory: () => signal<unknown>(SEGURA) }
);

/** Rechaza propiedades extra, valores arbitrarios, clases CSS y textos no confiables. */
export function normalizarPresentacionV5(valor: unknown): PresentacionContinuidadV5 {
  if (valor === null || typeof valor !== 'object' || Array.isArray(valor)) return SEGURA;
  const campos = valor as Record<string, unknown>;
  if (Object.keys(campos).length !== 2 ||
      !Object.prototype.hasOwnProperty.call(campos, 'marca') ||
      !Object.prototype.hasOwnProperty.call(campos, 'densidad')) return SEGURA;
  const marca = campos['marca'];
  const densidad = campos['densidad'];
  if ((marca !== 'solqaryn' && marca !== 'institucional') ||
      (densidad !== 'normal' && densidad !== 'compacta')) return SEGURA;
  return Object.freeze({ marca, densidad });
}
