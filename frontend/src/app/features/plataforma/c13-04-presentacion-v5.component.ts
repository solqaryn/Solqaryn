import { Component, ChangeDetectionStrategy, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export type MarcaC1304 = 'institucional' | 'contraste';
export type DensidadC1304 = 'comoda' | 'compacta';
export interface PresentacionC1304 {
  readonly marca: MarcaC1304;
  readonly densidad: DensidadC1304;
}
const DEFAULT: PresentacionC1304 = Object.freeze({ marca: 'institucional', densidad: 'comoda' });

/** Fail-closed once-only snapshot of unknown appearance; no CSS/URL/HTML is trusted. */
export function normalizarPresentacionC1304(value: unknown): PresentacionC1304 {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) return DEFAULT;
  try {
    const marca: unknown = Reflect.get(value, 'marca');
    const densidad: unknown = Reflect.get(value, 'densidad');
    if ((marca === 'institucional' || marca === 'contraste') &&
        (densidad === 'comoda' || densidad === 'compacta')) {
      return Object.freeze({ marca, densidad });
    }
  } catch { /* revoked proxy / throwing getter: safe default */ }
  return DEFAULT;
}

/** C13-04.M04.03 cosmetic, non-authoritative platform-only reference. */
@Component({
  selector: 'app-c1304-presentacion-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './c13-04-presentacion-v5.component.html',
  styleUrls: ['./c13-04-presentacion-v5.component.css']
})
export class C1304PresentacionV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly presentacion = signal<PresentacionC1304>(DEFAULT);
  seleccionar(entrada: unknown): boolean {
    if (!this.autorizado()) return false;
    const next = normalizarPresentacionC1304(entrada);
    const prev = this.presentacion();
    if (next.marca === prev.marca && next.densidad === prev.densidad) return false;
    this.presentacion.set(next);
    return true;
  }
}
