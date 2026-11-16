import { ChangeDetectionStrategy, Component, computed, effect, inject, InjectionToken, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export interface CoberturaDimensionV5 {
  readonly clave: string;
  readonly titulo: string;
  readonly descripcion: string;
}

/** Solo permite sustituir el cargador declarativo en pruebas: nunca consulta Sheets ni certifica resultados. */
export const COBERTURA_FUENTE_V5 = new InjectionToken<() => Promise<readonly CoberturaDimensionV5[]>>('CoberturaFuenteV5');


/** M04.03: apariencia local allowlisted. Ningún valor afecta la autorización o la evidencia. */
export type CoberturaMarcaV5 = 'institucional' | 'neutra';
export type CoberturaDensidadV5 = 'regular' | 'compacta';
export interface CoberturaPresentacionV5 {
  readonly marca: CoberturaMarcaV5;
  readonly densidad: CoberturaDensidadV5;
}
export const COBERTURA_PRESENTACION_V5 = new InjectionToken<unknown>('CoberturaPresentacionV5');
const PRESENTACION_INSTITUCIONAL_V5: CoberturaPresentacionV5 = Object.freeze({ marca: 'institucional', densidad: 'regular' });
const PRESENTACION_NEUTRA_V5: CoberturaPresentacionV5 = Object.freeze({ marca: 'neutra', densidad: 'regular' });

/** Los objetos arbitrarios, estilos, clases y cadenas de entrada nunca pasan al template. */
export function normalizarCoberturaPresentacionV5(valor: unknown): CoberturaPresentacionV5 {
  if (valor === null || valor === undefined) return PRESENTACION_INSTITUCIONAL_V5;
  if (typeof valor !== 'object') return PRESENTACION_NEUTRA_V5;
  let marca: unknown;
  let densidad: unknown;
  try {
    // Un getter/proxy no puede cambiar la marca validada antes de construir el DOM.
    if (Array.isArray(valor)) return PRESENTACION_NEUTRA_V5;
    marca = Reflect.get(valor, 'marca');
    densidad = Reflect.get(valor, 'densidad');
  } catch {
    // Un getter que falla o un Proxy revocado se trata como entrada hostil.
    return PRESENTACION_NEUTRA_V5;
  }
  if (
    (marca !== 'institucional' && marca !== 'neutra') ||
    (densidad !== 'regular' && densidad !== 'compacta')
  ) return PRESENTACION_NEUTRA_V5;
  return Object.freeze({ marca, densidad });
}

/** C15-01.M04.01/M04.02: cobertura declarativa con estados locales, sin resultados de auditoría ficticios. */
@Component({
  selector: 'app-cobertura-trazabilidad-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="coverage-shell" aria-labelledby="coverage-title"
      [class.coverage-neutral]="presentacion.marca === 'neutra'"
      [class.coverage-compacta]="presentacion.densidad === 'compacta'"
      [attr.data-presentacion-marca]="presentacion.marca"
      [attr.data-presentacion-densidad]="presentacion.densidad">
      <header>
        <p class="eyebrow">{{ presentacion.marca === 'institucional' ? 'SOLQARYN' : 'Referencia' }} · Cobertura y trazabilidad</p>
        <h1 id="coverage-title">Revisión de cobertura del plan</h1>
        <p>Vista interna de referencia; no sustituye PLAN_MAESTRO, COLA, DAG_TECNICO ni BITACORA.</p>
        <a routerLink="/plataforma" aria-label="Regresar a administración de plataforma">Volver a plataforma</a>
      </header>
      @if (accesoVerificado()) {
        <section class="notice" aria-label="Limitaciones de evidencia">
          <h2>Sin certificación automática</h2>
          <p role="status">Requisitos huérfanos, duplicados y cumplimiento global: NO_MEDIDO.</p>
          <p>La comparación causal requiere las fuentes institucionales, IDs exactos, CI y A5 de un revisor independiente.</p>
        </section>
        <section aria-labelledby="coverage-dimensions">
          <h2 id="coverage-dimensions">Dimensiones que deben contrastarse</h2>
          @switch (estadoVista()) {
            @case ('cargando') {
              <p aria-live="polite" data-testid="c1501-loading">Preparando las dimensiones locales de referencia…</p>
            }
            @case ('vacio') {
              <p aria-live="polite" data-testid="c1501-empty">No hay dimensiones locales disponibles. No se ha certificado el plan.</p>
            }
            @case ('error') {
              <div class="notice" role="alert" data-testid="c1501-error">
                <p>La lectura local de referencia falló. No se mostrarán datos incompletos.</p>
                <button type="button" class="retry" (click)="reintentarLectura()">Reintentar lectura local</button>
              </div>
            }
            @case ('listo') {
              <p aria-live="polite" data-testid="c1501-loaded">Dimensiones locales cargadas; cobertura institucional pendiente de comprobación.</p>
              <ul class="coverage-list">
                @for (dimension of dimensionesVisibles(); track dimension.clave) {
                  <li>
                    <h3>{{ dimension.titulo }}</h3>
                    <p>{{ dimension.descripcion }}</p>
                    <span class="unverified" aria-label="Evidencia no verificada">SIN_VERIFICAR</span>
                  </li>
                }
              </ul>
            }
          }
        </section>
        <p role="note">No hay operaciones de aprobación, edición, exportación, asignación o despliegue.</p>
      } @else {
        <p role="alert" class="notice">Acceso no autorizado. Los controles de cobertura permanecen ocultos.</p>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface,#17212d); }
    .coverage-shell { box-sizing:border-box; padding:clamp(1rem,3vw,2.5rem); margin:auto; max-width:70rem; }
    h1 { font-size:clamp(1.6rem,4vw,2.25rem); line-height:1.2; }
    h2 { font-size:1.25rem; }
    h3 { font-size:1rem; margin:0 0 .4rem; }
    p { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { color:var(--mat-sys-primary,#2157a3); font-weight:700; }
    header a { display:inline-block; padding:.7rem .85rem; min-height:44px; box-sizing:border-box; border:1px solid currentColor; border-radius:.55rem; color:inherit; text-decoration:underline; }
    header a:focus-visible, .retry:focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
    .retry { display:inline-block; min-height:44px; padding:.6rem .8rem; border:1px solid currentColor; border-radius:.5rem; background:transparent; color:inherit; cursor:pointer; }
    .notice { border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.7rem; padding:1rem; margin:1rem 0; }
    .coverage-neutral .eyebrow { color:var(--mat-sys-on-surface,#25313e); }
    .coverage-compacta .coverage-list li { padding:.75rem; }
    .coverage-compacta .notice { margin:.75rem 0; }
    .coverage-list { list-style:none; padding:0; display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:.8rem; }
    .coverage-list li { min-width:0; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.7rem; padding:1rem; }
    .unverified { display:inline-block; font-size:.8rem; font-weight:700; letter-spacing:.02em; border:1px solid currentColor; border-radius:.4rem; padding:.25rem .45rem; }
    @media(max-width:45rem) { .coverage-list { grid-template-columns:1fr; } }
  `]
})
export class CoberturaTrazabilidadV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly fuente = inject(COBERTURA_FUENTE_V5, { optional: true });
  readonly presentacion = normalizarCoberturaPresentacionV5(inject(COBERTURA_PRESENTACION_V5, { optional: true }));
  private versionLectura = 0;
  readonly estadoVista = signal<'cargando' | 'vacio' | 'error' | 'listo' | 'denegado'>('cargando');
  readonly dimensionesVisibles = signal<readonly CoberturaDimensionV5[]>([]);
  readonly accesoVerificado = computed(() => {
    const s = this.plataforma.sesion();
    return s?.ambito === 'Plataforma' && s.esAdministrador === true;
  });

  /** Dimensiones, no resultados del plan; nunca inventar contadores. */
  readonly dimensiones = [
    { clave: 'requisitos', titulo: 'Requisitos y no-alcance', descripcion: 'Relacionar IDs del plan con el documento ejecutable vigente.' },
    { clave: 'dependencias', titulo: 'Dependencias y DAG', descripcion: 'Revisar puertas técnicas, estados, propietarios y predecesores A5.' },
    { clave: 'evidencia', titulo: 'Evidencia y pruebas', descripcion: 'Comprobar diff, SHA causal, pruebas pertinentes y seguridad tenant.' },
    { clave: 'historial', titulo: 'Bitácora y revisión', descripcion: 'Identificar eventos idempotentes, revisión independiente y rollback.' }
  ] as const;

  constructor() {
    effect(() => {
      if (!this.accesoVerificado()) {
        ++this.versionLectura;
        this.dimensionesVisibles.set([]);
        this.estadoVista.set('denegado');
        return;
      }
      void this.reintentarLectura();
    });
  }

  /** Checkpoint de UI local: sin HTTP, sin mutaciones y sin resultados del Sheet. */
  async reintentarLectura(): Promise<void> {
    if (!this.accesoVerificado()) return;
    const version = ++this.versionLectura;
    this.estadoVista.set('cargando');
    this.dimensionesVisibles.set([]);
    try {
      const datos = await (this.fuente ? this.fuente() : Promise.resolve(this.dimensiones));
      if (version !== this.versionLectura || !this.accesoVerificado()) return;
      if (!Array.isArray(datos) || datos.some(item =>
        !item || typeof item.clave !== 'string' || !item.clave.trim() ||
        typeof item.titulo !== 'string' || !item.titulo.trim() ||
        typeof item.descripcion !== 'string' || !item.descripcion.trim()
      )) throw new Error('INVALID_LOCAL_COVERAGE_DIMENSIONS');
      this.dimensionesVisibles.set(datos.map(item => ({ ...item })));
      this.estadoVista.set(datos.length === 0 ? 'vacio' : 'listo');
    } catch {
      if (version !== this.versionLectura || !this.accesoVerificado()) return;
      this.dimensionesVisibles.set([]);
      this.estadoVista.set('error');
    }
  }
}
