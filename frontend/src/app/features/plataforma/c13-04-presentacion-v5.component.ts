import { Component, ChangeDetectionStrategy, DestroyRef, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { defer } from 'rxjs';
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
  if (value === null || typeof value !== 'object') return DEFAULT;
  try {
    // Array.isArray can throw on revoked proxies, so guard it as untrusted input.
    if (Array.isArray(value)) return DEFAULT;
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
  private readonly destroyRef = inject(DestroyRef);

  // C13-04.M04.02: la verificación de sesión no es verificación del catálogo.
  readonly comprobacion = signal<'vacio' | 'cargando' | 'verificada' | 'denegada' | 'error'>('vacio');
  readonly estadoInventario = computed(() => {
    const estado = this.comprobacion();
    if (estado === 'cargando') return 'cargando';
    // 503 de sesión revocada sigue siendo error técnico, nunca autorización exitosa.
    if (estado === 'error') return 'error';
    if (estado === 'denegada' || !this.autorizado()) return 'sin_permiso';
    return estado === 'verificada' ? 'exito' : 'vacio';
  });

  revalidarEstadoGlobal(): void {
    if (!this.autorizado() || this.comprobacion() === 'cargando') return;
    this.comprobacion.set('cargando');
    // El servicio borra la sesión local y reconsulta al backend; la UI no certifica
    // módulos, planes, CI ni permisos empresariales por un resultado sintético.
    defer(() => this.plataforma.obtener()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.comprobacion.set(this.autorizado() ? 'verificada' : 'denegada'),
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : 0;
        this.comprobacion.set(status === 401 || status === 403 ? 'denegada' : 'error');
      },
      complete: () => {
        if (this.comprobacion() === 'cargando') this.comprobacion.set('error');
      }
    });
  }
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
