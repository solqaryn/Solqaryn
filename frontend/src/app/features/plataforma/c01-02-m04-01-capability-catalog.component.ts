import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, type Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

/** Inventario derivado del router runtime, incluyendo rutas feature con ...spread.
 * Solo metadatos estaticos de rutas protegidas, nunca grants backend. */
export function c0102CatalogoDeRutas(configuracion: Routes) {
  return configuracion
    .filter(ruta =>
      ruta.canActivate?.includes(authGuard) === true &&
      ruta.canActivate?.includes(permisoGuard) === true &&
      typeof ruta.path === 'string' &&
      typeof ruta.data?.['modulo'] === 'string' &&
      typeof ruta.data?.['accion'] === 'string')
    .map(ruta => ({
      ruta: '/' + ruta.path,
      modulo: ruta.data!['modulo'] as string,
      accion: ruta.data!['accion'] as string
    }));
}

@Component({
  selector: 'app-c0102-capability-catalog',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="catalogo" aria-labelledby="c0102-title">
      <a routerLink="/plataforma">Volver a plataforma</a>
      <h1 id="c0102-title">Catálogo global de módulos y capacidades</h1>
      <p role="note">Información de rutas protegidas, no fuente de permisos. La autorización efectiva se resuelve exclusivamente en el servidor.</p>
      @if (!autorizado()) {
        <p role="alert" data-testid="c0102-oculto">No se verificó autoridad de plataforma. Catálogo oculto.</p>
      } @else {
        <ol data-testid="c0102-listado">
          @for (fila of filas(); track fila.ruta) {
            <li>
              <h2>{{ fila.modulo }} · {{ fila.accion }}</h2>
              <p>Ruta: {{ fila.ruta }}</p>
              <p [attr.data-evidence-state]="fila.estado">{{ fila.estado }} — {{ fila.causa }}</p>
            </li>
          }
        </ol>
      }
    </main>
  `,
  styles: [`
    .catalogo { max-width: 1120px; padding: clamp(16px, 3vw, 32px); margin: auto; }
    ol { list-style: none; display: grid; gap: 16px; padding: 0; grid-template-columns: repeat(auto-fit,minmax(min(100%,260px),1fr)); }
    li { border: 1px solid var(--color-border, #8193a5); border-radius: 12px; padding: 14px; overflow-wrap: anywhere; }
    h2 { font-size: 1rem; } a:focus-visible { outline: 3px solid var(--color-primary,#0868ad); outline-offset: 4px; }
  `]
})
export class C0102CapabilityCatalogComponent {
  private readonly router = inject(Router);
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly filas = computed(() => {
    if (!this.autorizado()) return [];
    return c0102CatalogoDeRutas(this.router.config).map(ruta => {
      const base = { ...ruta };
      if (!this.tenant.tieneContextoVerificado())
        return { ...base, estado: 'SIN_TENANT_VERIFICADO', causa: 'No hay contexto y membresía empresarial verificados.' };
      if (!this.permisos.cargado())
        return { ...base, estado: 'PERMISOS_NO_CONFIRMADOS', causa: 'La API no confirmó permisos del contexto actual.' };
      if (!this.permisos.puede(ruta.modulo, ruta.accion))
        return { ...base, estado: 'PERMISO_O_CAPACIDAD_NO_CONFIRMADA', causa: 'No se confirmó simultáneamente permiso y capacidad; plan/rol/rubro no identificables desde esta señal.' };
      if (!this.permisos.rutaHabilitada(ruta.ruta))
        return { ...base, estado: 'RUTA_RESTRINGIDA', causa: 'No se confirmó la ruta habilitada para este contexto.' };
      return { ...base, estado: 'SENAL_CLIENTE_NO_AUTORITATIVA', causa: 'Coinciden señales UI; cada solicitud todavía exige autorización de servidor.' };
    });
  });
}
