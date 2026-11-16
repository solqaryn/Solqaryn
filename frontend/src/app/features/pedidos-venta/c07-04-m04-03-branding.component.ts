import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

/** C07-04.M04.03: opciones de presentación locales, sin autoridad financiera. */
export type C0704Presentacion = Readonly<{
  marca: 'SOLQARYN' | 'SOLQARYN ERP';
  densidad: 'comoda' | 'compacta';
  contraste: 'normal' | 'alto';
}>;

/** No interpreta HTML, CSS, URL, etiquetas arbitrarias ni campos extraños. */
export function normalizarC0704Presentacion(entrada: unknown): C0704Presentacion {
  const v: Record<string, unknown> =
    entrada !== null && typeof entrada === 'object' && !Array.isArray(entrada)
      ? entrada as Record<string, unknown> : {};
  return {
    marca: v['marca'] === 'SOLQARYN ERP' ? 'SOLQARYN ERP' : 'SOLQARYN',
    densidad: v['densidad'] === 'compacta' ? 'compacta' : 'comoda',
    contraste: v['contraste'] === 'alto' ? 'alto' : 'normal'
  };
}

@Component({
  selector: 'app-c0704-branding-v5',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (autorizado()) {
      <section class="pedidos-branding-v5"
        [attr.data-densidad]="presentacion().densidad"
        [attr.data-contraste]="presentacion().contraste"
        aria-label="Presentación de pedidos de venta">
        <div class="presentacion-textos">
          <p class="presentacion-marca">{{ presentacion().marca }} · Pedidos de venta</p>
          <p>Preferencias visuales locales. Sin cambios de importes, permisos ni operaciones.</p>
        </div>
        <div class="presentacion-acciones" role="group" aria-label="Preferencias visuales">
          <button type="button" (click)="alternarDensidad()">
            {{ presentacion().densidad === 'comoda' ? 'Vista compacta' : 'Vista cómoda' }}
          </button>
          <button type="button" (click)="alternarContraste()">
            {{ presentacion().contraste === 'normal' ? 'Contraste alto' : 'Contraste normal' }}
          </button>
        </div>
      </section>
    }
  `,
  styles: [`
    :host { display:block; min-width:0; max-width:100%; }
    .pedidos-branding-v5 { box-sizing:border-box; display:flex; flex-wrap:wrap;
      align-items:center; justify-content:space-between; gap:.75rem; min-width:0;
      max-width:100%; margin:0 0 1rem; padding:1rem;
      border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.65rem; }
    .presentacion-textos { min-width:0; flex:1 1 14rem; }
    .presentacion-textos p { margin:0; line-height:1.45; overflow-wrap:anywhere; }
    .presentacion-marca { font-weight:650; margin-bottom:.25rem !important; }
    .presentacion-acciones { display:flex; flex-wrap:wrap; gap:.5rem; min-width:0; }
    button { min-height:44px; border:1px solid currentColor; border-radius:.45rem;
      padding:.45rem .8rem; background:transparent; color:inherit; cursor:pointer; }
    button:focus-visible { outline:3px solid currentColor; outline-offset:2px; }
    .pedidos-branding-v5[data-densidad="compacta"] { padding:.5rem .75rem; gap:.5rem; }
    .pedidos-branding-v5[data-contraste="alto"] { border-width:2px;
      background:var(--mat-sys-surface-container-high,#eee); }
    @media(max-width:24rem) { .presentacion-acciones { width:100%; }
      .presentacion-acciones button { flex:1 1 9rem; } }
  `]
})
export class C0704BrandingV5Component {
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  readonly autorizado = computed(() =>
    this.tenant.tieneContextoVerificado() &&
    this.permisos.puede('Ventas', 'Ver') &&
    this.permisos.rutaHabilitada('pedidos-venta')
  );
  readonly presentacion = signal<C0704Presentacion>(normalizarC0704Presentacion(null));

  alternarDensidad(): void {
    if (!this.autorizado()) return;
    this.presentacion.update(p => ({
      ...p, densidad: p.densidad === 'comoda' ? 'compacta' : 'comoda'
    }));
  }
  alternarContraste(): void {
    if (!this.autorizado()) return;
    this.presentacion.update(p => ({
      ...p, contraste: p.contraste === 'normal' ? 'alto' : 'normal'
    }));
  }
}
