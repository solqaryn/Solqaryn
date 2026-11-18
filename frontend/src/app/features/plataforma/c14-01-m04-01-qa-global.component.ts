import { ChangeDetectionStrategy, Component, InjectionToken, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** Estado visual de catalogo local, nunca estado CI/QA ni permiso backend. */
export interface C1401QaControl { readonly id: string; readonly titulo: string; readonly alcance: string; }
export type C1401QaPanelState = 'denegado' | 'cargando' | 'vacio' | 'error' | 'listo';
export const C1401_QA_CATALOG_LOADER = new InjectionToken<() => Promise<readonly C1401QaControl[]>>(
  'C1401_QA_CATALOG_LOADER',
  { providedIn: 'root', factory: () => () => import('./c14-01-m04-02-qa-catalog').then(m => m.qaControles) }
);

/** C14-01.M04.01 — UI QA privada, nunca fuente de autorizacion ni de PASS. */
@Component({
  selector: 'app-c1401-qa-global',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './c14-01-m04-01-qa-global.component.html',
  styleUrl: './c14-01-m04-01-qa-global.component.scss'
})
export class C1401QaGlobalComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly autorizado = computed(() => {
    const s = this.plataforma.sesion();
    return s?.ambito === 'Plataforma' && s.esAdministrador === true;
  });
  private readonly cargarCatalogo = inject(C1401_QA_CATALOG_LOADER);
  readonly controles = signal<readonly C1401QaControl[]>([]);
  readonly estado = signal<C1401QaPanelState>('denegado');

  constructor() {
    effect((onCleanup) => {
      if (!this.autorizado()) {
        this.controles.set([]);
        this.estado.set('denegado');
        return;
      }
      let vigente = true;
      this.controles.set([]);
      this.estado.set('cargando');
      onCleanup(() => { vigente = false; });
      void this.cargarCatalogo().then((items) => {
        if (!vigente || !this.autorizado()) return;
        if (!this.catalogoValido(items)) {
          this.controles.set([]);
          this.estado.set('error');
          return;
        }
        this.controles.set(items.map(item => ({ ...item })));
        this.estado.set(items.length === 0 ? 'vacio' : 'listo');
      }).catch(() => {
        if (!vigente || !this.autorizado()) return;
        this.controles.set([]);
        this.estado.set('error');
      });
    });
  }

  private catalogoValido(items: readonly C1401QaControl[]): boolean {
    if (!Array.isArray(items) || items.length > 20) return false;
    const ids = new Set<string>();
    return items.every(item => {
      const valido = item && typeof item.id === 'string' && /^[a-z0-9-]{1,40}$/.test(item.id)
        && !ids.has(item.id) && typeof item.titulo === 'string' && item.titulo.trim().length > 0
        && item.titulo.length <= 120 && typeof item.alcance === 'string'
        && item.alcance.trim().length > 0 && item.alcance.length <= 300;
      if (valido) ids.add(item.id);
      return valido;
    });
  }
}
