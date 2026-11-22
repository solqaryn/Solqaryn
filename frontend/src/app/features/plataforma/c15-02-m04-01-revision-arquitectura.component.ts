import { ChangeDetectionStrategy, Component, computed, effect, inject, InjectionToken, signal, DestroyRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export interface C1502Superficie {
  readonly id: string;
  readonly nombre: string;
  readonly objetivo: string;
}

/** Fuente inyectable SOLO para probar fronteras de lectura y errores.
 * La UI normal conserva cinco referencias locales; no consulta proveedores reales.
 */
export const C1502_SUPERFICIES_FUENTE = new InjectionToken<() => Promise<readonly C1502Superficie[]>>('C1502SuperficiesFuente');

/** V20 M04.03: entrada de branding puramente visual, sin CSS arbitrario ni elevacion de permisos. */
export type C1502Marca = 'institucional' | 'neutra';
export type C1502Densidad = 'regular' | 'compacta';
export interface C1502Presentacion {
  readonly marca: C1502Marca;
  readonly densidad: C1502Densidad;
}
export const C1502_PRESENTACION = new InjectionToken<unknown>('C1502PresentacionLocal');
const C1502_PRESENTACION_DEFAULT: C1502Presentacion = Object.freeze({
  marca: 'institucional', densidad: 'regular'
});
const C1502_PRESENTACION_NEUTRA: C1502Presentacion = Object.freeze({
  marca: 'neutra', densidad: 'regular'
});
export function normalizarC1502Presentacion(input: unknown): C1502Presentacion {
  if (input === null || input === undefined) return C1502_PRESENTACION_DEFAULT;
  if (typeof input !== 'object') return C1502_PRESENTACION_NEUTRA;
  let marca: unknown;
  let densidad: unknown;
  try {
    if (Array.isArray(input)) return C1502_PRESENTACION_NEUTRA;
    // Capturar valores una sola vez para evitar getter que cambie entre validacion y uso.
    marca = Reflect.get(input, 'marca');
    densidad = Reflect.get(input, 'densidad');
  } catch {
    return C1502_PRESENTACION_NEUTRA;
  }
  if ((marca !== 'institucional' && marca !== 'neutra') ||
      (densidad !== 'regular' && densidad !== 'compacta')) {
    return C1502_PRESENTACION_NEUTRA;
  }
  return Object.freeze({ marca, densidad });
}


/**
 * C15-02.M04.01 — solo presenta contratos de revisión documental DEV.
 * Esta pantalla NO conecta infra, no lee secretos y no concede permisos.
 * plataformaGuard revalida el servidor; el componente oculta contenido al revocar.
 */
@Component({
  selector: 'app-c1502-revision-arquitectura',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="review" aria-labelledby="review-title" data-testid="c1502-review"
      [class.review-neutral]="presentacion.marca === 'neutra'"
      [class.review-compacta]="presentacion.densidad === 'compacta'"
      [attr.data-marca]="presentacion.marca"
      [attr.data-densidad]="presentacion.densidad">
      <header>
        <p class="eyebrow">{{ presentacion.marca === 'institucional' ? 'SOLQARYN' : 'Referencia' }} · DEV · arquitectura</p>
        <h1 id="review-title">Revisión de arquitectura e integraciones</h1>
        <p>Inventario de evidencias requeridas, no comprobaciones productivas ejecutadas.</p>
        <a routerLink="/plataforma" aria-label="Volver a plataforma">Volver a plataforma</a>
      </header>
      @if (autorizado()) {
        <section aria-labelledby="scope-title">
          <h2 id="scope-title">Superficies por contrastar</h2>
          @switch (estadoVista()) {
            @case ('cargando') {
              <p role="status" aria-live="polite" data-testid="architecture-loading">Preparando referencias locales sin consultar proveedores…</p>
            }
            @case ('vacio') {
              <p role="status" data-testid="architecture-empty">No hay referencias locales disponibles. Nada certificado.</p>
            }
            @case ('error') {
              <div role="alert" data-testid="architecture-error">
                <p>La lectura local falló; no se muestran datos parciales ni detalles internos.</p>
                <button class="retry" type="button" (click)="reintentarLectura()">Reintentar lectura local</button>
              </div>
            }
            @case ('listo') {
              <p role="status" data-testid="architecture-loaded">Referencias locales cargadas; evidencias sin verificar.</p>
              <ul class="cards">
                @for (item of visibles(); track item.id) {
                  <li data-testid="architecture-item">
                    <h3>{{ item.nombre }}</h3>
                    <p>{{ item.objetivo }}</p>
                    <p class="pending"><strong>NO_VERIFICADO</strong> · sin acceso a proveedores reales</p>
                  </li>
                }
              </ul>
            }
          }
        </section>
        <aside role="note">
          Diagrama, contratos, rollback y control de secretos requieren evidencia independiente.
          Ningún cambio de DNS, hosting, R2, MySQL, credenciales, QA, main o producción está autorizado.
        </aside>
      } @else {
        <p role="alert" data-testid="architecture-denied">Acceso global no confirmado. Detalles ocultos.</p>
      }
    </section>
  `,
  styles: [`
    :host { display:block; min-width:0; }
    .review { box-sizing:border-box; width:min(calc(100% - 2rem),72rem); margin:0 auto; padding:clamp(1rem,3vw,2.25rem) 0 3rem; }
    .eyebrow { font-weight:700; color:var(--mat-sys-primary,#125b8a); }
    .review-neutral .eyebrow { color:var(--mat-sys-on-surface,#263442); }
    .review-compacta .cards li { padding:.75rem; }
    .review-compacta .cards { gap:.65rem; }
    h1 { font-size:clamp(1.5rem, calc(1.5rem + 1vw), 2.25rem); line-height:1.2; overflow-wrap:anywhere; }
    h2 { margin-top:1.5rem; }
    h3 { margin:.1rem 0 .5rem; font-size:1.08rem; }
    p,li,aside { line-height:1.55; overflow-wrap:anywhere; }
    a { display:inline-flex; min-height:44px; align-items:center; padding:.5rem .8rem; border:2px solid currentColor; border-radius:.5rem; }
    a:focus-visible, .retry:focus-visible { outline:3px solid var(--mat-sys-primary,#125b8a); outline-offset:3px; }
    .retry { display:inline-flex; min-height:44px; align-items:center; border:2px solid currentColor; border-radius:.5rem; background:transparent; color:inherit; padding:.6rem .8rem; cursor:pointer; }
    .cards { display:grid; grid-template-columns:repeat(auto-fit,minmax(min(100%,16rem),1fr)); gap:.85rem; padding:0; list-style:none; }
    .cards li { min-width:0; padding:1rem; border:1px solid var(--mat-sys-outline,#76818a); border-radius:.7rem; }
    .pending { border-inline-start:3px solid var(--mat-sys-outline,#76818a); padding-inline-start:.65rem; }
    aside { margin-top:1.4rem; padding:.85rem; border:1px solid var(--mat-sys-outline,#76818a); border-radius:.5rem; }
    @media (max-width:480px) { .review { width:calc(100% - 1.5rem); } }
  `]
})
export class C1502RevisionArquitecturaComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly fuente = inject(C1502_SUPERFICIES_FUENTE, { optional: true });
  private readonly destroyRef = inject(DestroyRef);
  readonly presentacion = normalizarC1502Presentacion(inject(C1502_PRESENTACION, { optional: true }));
  private versionLectura = 0;

  readonly superficies: readonly C1502Superficie[] = [
    { id: 'hosting', nombre: 'Hosting y ejecución', objetivo: 'Contrastar topología, versionado y rollback sin activar despliegues.' },
    { id: 'cdn', nombre: 'Cloudflare y DNS', objetivo: 'Revisar rutas, registros y límites sin modificar dominios.' },
    { id: 'objects', nombre: 'R2 y archivos', objetivo: 'Examinar contratos de acceso sin credenciales ni copias reales.' },
    { id: 'database', nombre: 'MySQL y datos', objetivo: 'Identificar migraciones, integridad y restauración pendientes de pruebas.' },
    { id: 'providers', nombre: 'Proveedores e integración', objetivo: 'Validar dependencias, fallos seguros y evidencias antes de conectar servicios.' }
  ];
  readonly visibles = signal<readonly C1502Superficie[]>([]);
  readonly estadoVista = signal<'cargando' | 'vacio' | 'error' | 'listo' | 'denegado'>('denegado');
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });

  constructor() {
    effect(() => {
      if (!this.autorizado()) {
        this.versionLectura++;
        this.visibles.set([]);
        this.estadoVista.set('denegado');
        return;
      }
      if (this.fuente === null) {
        this.versionLectura++;
        this.visibles.set(this.superficies);
        this.estadoVista.set('listo');
        return;
      }
      this.cargar();
    });
    this.destroyRef.onDestroy(() => { this.versionLectura++; });
  }

  reintentarLectura(): void {
    if (this.autorizado() && this.fuente !== null) this.cargar();
  }

  private cargar(): void {
    if (!this.autorizado() || !this.fuente) return;
    const version = ++this.versionLectura;
    this.visibles.set([]);
    this.estadoVista.set('cargando');
    Promise.resolve().then(() => this.fuente!()).then(
      (datos) => {
        if (version !== this.versionLectura || !this.autorizado()) return;
        const validos = this.normalizar(datos);
        this.visibles.set(validos);
        this.estadoVista.set(validos.length ? 'listo' : 'vacio');
      },
      () => {
        if (version !== this.versionLectura || !this.autorizado()) return;
        this.visibles.set([]);
        this.estadoVista.set('error');
      }
    ).catch(() => {
      if (version !== this.versionLectura || !this.autorizado()) return;
      this.visibles.set([]);
      this.estadoVista.set('error');
    });
  }

  /** Nunca interpolar excepciones, objetos externos ni respuestas incompletas. */
  private normalizar(entrada: unknown): readonly C1502Superficie[] {
    if (!Array.isArray(entrada) || entrada.length > 16) throw new Error('Fuente local no válida.');
    const ids = new Set<string>();
    const resultado: C1502Superficie[] = [];
    for (const item of entrada) {
      if (resultado.length >= 16 || !item || typeof item !== 'object') throw new Error('Registro inválido.');
      const id = (item as C1502Superficie).id;
      const nombre = (item as C1502Superficie).nombre;
      const objetivo = (item as C1502Superficie).objetivo;
      if (typeof id !== 'string' || !/^[a-z][a-z0-9-]{0,63}$/.test(id) || ids.has(id) ||
          typeof nombre !== 'string' || !nombre.trim() || nombre.length > 120 ||
          typeof objetivo !== 'string' || !objetivo.trim() || objetivo.length > 300) {
        throw new Error('Registro mal formado.');
      }
      ids.add(id);
      resultado.push(Object.freeze({ id, nombre: nombre.trim(), objetivo: objetivo.trim() }));
    }
    return Object.freeze(resultado);
  }
}
