import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

export type C1404ReleasePresentacion = Readonly<{
  marca: 'SOLQARYN' | 'SOLQARYN DEV';
  densidad: 'comoda' | 'compacta';
  contraste: 'normal' | 'alto';
}>;

/** C14-04.M04.03: solo presets locales; rechazar CSS/HTML/URL y flags arbitrarios. */
export function normalizarC1404ReleasePresentacion(value: unknown): C1404ReleasePresentacion {
  const raw: Record<string, unknown> =
    value !== null && typeof value === 'object' && !Array.isArray(value)
      ? value as Record<string, unknown> : {};
  return {
    marca: raw['marca'] === 'SOLQARYN DEV' ? 'SOLQARYN DEV' : 'SOLQARYN',
    densidad: raw['densidad'] === 'compacta' ? 'compacta' : 'comoda',
    contraste: raw['contraste'] === 'alto' ? 'alto' : 'normal'
  };
}

/** Subpanel de presentación sin capacidad de cambiar release, DNS, backup o CI. */
@Component({
  selector: 'app-c1404-release-branding-v17',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (autorizado()) {
      <section class="release-branding-v17" data-testid="release-branding-v17"
        [attr.data-density]="presentacion().densidad"
        [attr.data-contrast]="presentacion().contraste"
        aria-label="Opciones de presentación de preparación DEV">
        <div class="release-branding-heading">
          <h2>{{ presentacion().marca }} · Preparación de salida</h2>
          <p>Preferencias visuales de consulta. Smoke, alertas y respaldos: NO_MEDIDO.</p>
        </div>
        <div class="release-branding-buttons" role="group" aria-label="Presentación local">
          <button type="button" (click)="alternarDensidad()">
            {{ presentacion().densidad === 'comoda' ? 'Densidad compacta' : 'Densidad cómoda' }}
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
    .release-branding-v17 { box-sizing:border-box; min-width:0; max-width:100%;
      display:flex; flex-wrap:wrap; justify-content:space-between; align-items:center;
      gap:.8rem; margin:0 0 1rem; padding:1rem; border:1px solid
      var(--mat-sys-outline-variant,#aab4bf); border-radius:.65rem; }
    .release-branding-heading { flex:1 1 15rem; min-width:0; }
    h2 { margin:0 0 .25rem; font-size:1.1rem; line-height:1.3; overflow-wrap:anywhere; }
    p { margin:0; line-height:1.5; overflow-wrap:anywhere; }
    .release-branding-buttons { display:flex; gap:.5rem; flex-wrap:wrap; min-width:0; }
    button { min-height:44px; padding:.45rem .75rem; border:1px solid currentColor;
      border-radius:.5rem; background:transparent; color:inherit; cursor:pointer; }
    button:focus-visible { outline:3px solid currentColor; outline-offset:2px; }
    .release-branding-v17[data-density="compacta"] { padding:.5rem .75rem; gap:.4rem; }
    .release-branding-v17[data-contrast="alto"] { border-width:2px;
      background:var(--mat-sys-surface-container-high,#eee); }
    @media(max-width:24rem) { .release-branding-buttons { width:100%; }
      .release-branding-buttons button { flex:1 1 8rem; } }
  `]
})
export class C1404ReleaseBrandingV17Component {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly autorizado = computed(() => {
    const s = this.plataforma.sesion();
    return s?.ambito === 'Plataforma' && s.esAdministrador === true;
  });
  readonly presentacion = signal<C1404ReleasePresentacion>(
    normalizarC1404ReleasePresentacion(null)
  );
  alternarDensidad(): void {
    if (!this.autorizado()) return;
    this.presentacion.update(x => ({
      ...x, densidad: x.densidad === 'comoda' ? 'compacta' : 'comoda'
    }));
  }
  alternarContraste(): void {
    if (!this.autorizado()) return;
    this.presentacion.update(x => ({
      ...x, contraste: x.contraste === 'normal' ? 'alto' : 'normal'
    }));
  }
}
