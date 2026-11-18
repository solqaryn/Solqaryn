import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, distinctUntilChanged, switchMap, tap } from 'rxjs';
import { Router, RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { c0102CatalogoDeRutas } from './c01-02-m04-01-capability-catalog.component';

export type EstadoCapacidad = 'DENEGADO' | 'SIN_TENANT' | 'CARGANDO' | 'ERROR' | 'VACIO' | 'SIN_PERMISOS' | 'EXITO';
export function resolverEstadoCapacidad(x: {
  autorizado: boolean; tenant: boolean; carga: 'cargando' | 'lista' | 'error';
  permisos: boolean; total: number; visibles: number;
}): EstadoCapacidad {
  if (!x.autorizado) return 'DENEGADO';
  if (!x.tenant) return 'SIN_TENANT';
  if (x.carga === 'cargando') return 'CARGANDO';
  if (x.carga === 'error' || !x.permisos) return 'ERROR';
  if (x.total === 0) return 'VACIO';
  return x.visibles > 0 ? 'EXITO' : 'SIN_PERMISOS';
}
@Component({
  selector: 'app-c0102-capability-states', standalone: true,
  imports: [RouterLink], changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="estados" aria-labelledby="estados-title">
      <a routerLink="/plataforma">Volver a plataforma</a>
      <h1 id="estados-title">Estado de capacidades y permisos</h1>
      <p role="note">Información de cliente. Cada solicitud exige autorización del servidor.</p>
      @switch (estado()) {
        @case ('DENEGADO') { <p role="alert" data-testid="estado-denegado">Sin autorización de plataforma.</p> }
        @case ('SIN_TENANT') { <p role="alert" data-testid="estado-sin-tenant">No hay empresa y membresía verificadas.</p> }
        @case ('CARGANDO') { <p role="status" aria-live="polite" data-testid="estado-cargando">Consultando permisos…</p> }
        @case ('ERROR') { <p role="alert" data-testid="estado-error">No se confirmó la autorización. Acceso denegado.</p> }
        @case ('VACIO') { <p role="status" data-testid="estado-vacio">No existen rutas protegidas.</p> }
        @case ('SIN_PERMISOS') { <p role="alert" data-testid="estado-sin-permisos">No hay rutas con permiso, capacidad y acceso verificados.</p> }
        @case ('EXITO') {
          <p role="status" data-testid="estado-exito">Señales UI confirmadas; el servidor autoriza cada acción.</p>
          <ul data-testid="estados-listado">
            @for (r of visibles(); track $index) { <li>{{ r.modulo }} · {{ r.accion }} — {{ r.ruta }}</li> }
          </ul>
        }
      }
    </main>`,
  styles: [`
    .estados { margin: auto; max-width: 1080px; padding: clamp(16px, 3vw, 32px); }
    ul { display: grid; gap: 8px; overflow-wrap: anywhere; }
    a:focus-visible { outline: 3px solid var(--color-primary, #0868ad); outline-offset: 4px; }
  `]
})
export class C0102CapabilityStatesComponent {
  private readonly router = inject(Router);
  private readonly sesion = inject(PlataformaSesionService);
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly carga = signal<'cargando' | 'lista' | 'error'>('cargando');
  private readonly empresaAcreditada = signal<number | null>(null);
  readonly autorizado = computed(() => {
    const s = this.sesion.sesion();
    return s?.ambito === 'Plataforma' && s.esAdministrador === true;
  });
  readonly catalogo = computed(() => c0102CatalogoDeRutas(this.router.config));
  readonly visibles = computed(() => {
    if (!this.autorizado() || !this.tenant.tieneContextoVerificado() ||
        this.empresaAcreditada() !== this.tenant.empresaIdVerificada() ||
        this.carga() !== 'lista' || !this.permisos.cargado()) return [];
    return this.catalogo().filter(x => this.permisos.puede(x.modulo, x.accion) &&
      this.permisos.rutaHabilitada(x.ruta));
  });
  readonly estado = computed(() => resolverEstadoCapacidad({
    autorizado: this.autorizado(), tenant: this.tenant.tieneContextoVerificado(),
    carga: this.carga() === 'error' ? 'error' :
      this.empresaAcreditada() === this.tenant.empresaIdVerificada() ? this.carga() : 'cargando',
    permisos: this.permisos.cargado(),
    total: this.catalogo().length, visibles: this.visibles().length
  }));
  // Las senales de plataforma y empresa mandan sobre respuestas de permisos anteriores.
  // Un cambio A/B cancela la suscripcion anterior y exige acreditar la empresa B.
  private readonly contextoReactivo = computed(() => ({
    empresa: this.tenant.empresaIdVerificada(),
    verificado: this.tenant.tieneContextoVerificado(),
    autorizado: this.autorizado(),
    permisosListos: this.permisos.cargado()
  }));

  constructor() {
    toObservable(this.contextoReactivo).pipe(
      distinctUntilChanged((a, b) =>
        a.empresa === b.empresa && a.verificado === b.verificado &&
        a.autorizado === b.autorizado && a.permisosListos === b.permisosListos),
      switchMap(actual => {
        this.empresaAcreditada.set(null);
        this.carga.set('cargando');
        if (!actual.autorizado || !actual.verificado || !actual.empresa) return EMPTY;
        const empresa = actual.empresa;
        if (actual.permisosListos) {
          this.empresaAcreditada.set(empresa);
          this.carga.set('lista');
          return EMPTY;
        }
        return this.permisos.cargar().pipe(
          tap(ok => {
            if (!this.contextoCoincide(empresa)) return;
            if (ok && this.permisos.cargado()) {
              this.empresaAcreditada.set(empresa);
              this.carga.set('lista');
            } else {
              this.carga.set('error');
            }
          }),
          catchError(() => {
            if (this.contextoCoincide(empresa)) this.carga.set('error');
            return EMPTY;
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();
  }

  private contextoCoincide(empresa: number): boolean {
    return this.autorizado() && this.tenant.tieneContextoVerificado() &&
      this.tenant.empresaIdVerificada() === empresa;
  }
}
