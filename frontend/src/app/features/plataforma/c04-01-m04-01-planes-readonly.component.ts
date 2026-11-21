import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C04-01.M04.01: superficie privada de referencia, no tarifario ni autorizador comercial. */
const SECCIONES = Object.freeze([
  Object.freeze({ clave: 'versiones', titulo: 'Versiones y vigencia', detalle: 'Pendiente de política comercial y versión inmutable aprobada.' }),
  Object.freeze({ clave: 'capacidades', titulo: 'Capacidades y límites', detalle: 'Cada límite requiere comprobación efectiva en el servidor por empresa y permiso.' }),
  Object.freeze({ clave: 'transiciones', titulo: 'Cambios y conservación', detalle: 'Upgrade, downgrade y reversión permanecen sin acciones disponibles en esta vista.' })
] as const);

@Component({
  selector: 'app-c0401-planes-readonly',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="planes" aria-labelledby="c0401-title" data-testid="c0401-planes-reference">
      <header>
        <p class="eyebrow">SOLQARYN · DEV · referencia privada</p>
        <h1 id="c0401-title">Catálogo de planes</h1>
        <p>Vista informativa sin tarifa aprobada, cobros ni cambios de suscripción.</p>
        <a routerLink="/plataforma" aria-label="Volver a plataforma">Volver a plataforma</a>
      </header>
      @if (autorizado()) {
        <section class="panel" aria-labelledby="c0401-scope" data-testid="c0401-private">
          <h2 id="c0401-scope">Alcance de referencia</h2>
          <p role="status" data-testid="c0401-unapproved">SIN_POLITICA_COMERCIAL_APROBADA · NO_CERTIFICADO</p>
          <ol aria-label="Contratos previstos del catálogo">
            @for (seccion of secciones; track seccion.clave) {
              <li [attr.data-testid]="'c0401-section-' + seccion.clave">
                <h3>{{ seccion.titulo }}</h3>
                <p>{{ seccion.detalle }}</p>
              </li>
            }
          </ol>
          <p role="note">Sin datos empresariales, precio, moneda, impuesto, PSP ni conexión con el catálogo comercial real. La autoridad JWT/RBAC/tenant se valida exclusivamente en el backend.</p>
        </section>
      } @else {
        <p role="alert" data-testid="c0401-denied">Autoridad de plataforma no confirmada. La referencia privada está oculta.</p>
      }
    </main>
  `,
  styles: [`
    :host { display: block; color: var(--mat-sys-on-surface, #202631); }
    .planes { box-sizing: border-box; margin: 0 auto; max-width: 72rem; padding: clamp(1rem, 3vw, 2.5rem); }
    h1 { font-size: clamp(1.55rem, 3vw, 2.2rem); line-height: 1.2; }
    h2 { font-size: 1.2rem; } h3 { margin: 0; font-size: 1.04rem; }
    p, li { line-height: 1.55; overflow-wrap: anywhere; }
    .eyebrow { font-weight: 700; }
    .panel { margin-top: 1.5rem; border: 1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius: .75rem; padding: 1rem; }
    ol { list-style: none; display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 250px), 1fr)); gap: 1rem; padding: 0; margin: 1rem 0; }
    li { min-width: 0; border: 1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius: .65rem; padding: 1rem; }
    a { display: inline-block; box-sizing: border-box; min-height: 44px; padding: .75rem 1rem; border: 1px solid currentColor; border-radius: .5rem; color: inherit; }
    a:focus-visible { outline: 3px solid var(--mat-sys-primary, #2157a3); outline-offset: 3px; }
  `]
})
export class C0401PlanesReadonlyComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly secciones = SECCIONES;
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
}
