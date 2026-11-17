import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { inventariarRutasV19, PROGRAMAS_PLAN_V19 } from './c13-04-m04-01-global-inventory';

/** Superficie del plano global solo lectura. El guard del router es adicional al filtro local;
 * cada ruta destino mantiene su propia autorización backend/guard.
 */
@Component({
  selector: 'app-c1304-inventario-global',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="inventario" aria-labelledby="titulo-inventario">
      <header>
        <p class="etiqueta">SOLQARYN · Plano global · DEV</p>
        <h1 id="titulo-inventario">Inventario de programas y rutas declaradas</h1>
        <p>Registro de configuración, no certificación funcional, permisos ni despliegues.</p>
        <a routerLink="/plataforma/consola-global-v5">Volver a consola global</a>
      </header>
      @if (!autorizado()) {
        <p role="alert">Acceso global no confirmado; inventario privado oculto.</p>
      } @else {
        <section aria-labelledby="programas-v19">
          <h2 id="programas-v19">Programas del plan: {{ programas.length }} de 16 identificadores</h2>
          <p role="note">La presencia en el plan no demuestra un módulo implementado ni un gate A5 aprobado.</p>
          <ol class="programas">
            @for (p of programas; track p.id) {
              <li><strong>{{ p.id }}</strong> — {{ p.nombre }} <span class="pendiente">Estado de cobertura: SIN_VERIFICAR</span></li>
            }
          </ol>
        </section>
        <section aria-labelledby="rutas-v19">
          <h2 id="rutas-v19">Entradas de Angular Router: {{ rutasVisibles().length }}</h2>
          <p>Incluye rutas hijas y arrays importados por spreads. Parámetros de detalle/edición no tienen enlaces inventados.</p>
          @if (lazyPendiente() > 0) {
            <p role="status">Subárboles lazy sin resolver: {{ lazyPendiente() }}; cobertura profunda NO_VERIFICADA.</p>
          }
          <p role="note">El inventario describe rutas declaradas, no las pantallas ejecutadas, WCAG, permisos HTTP ni resultados de pruebas.</p>
          <ol class="rutas" data-testid="c1304-route-inventory">
            @for (ruta of rutasVisibles(); track ruta.id) {
              <li>
                <span class="ruta">{{ ruta.path }}</span>
                <span class="meta">{{ ruta.superficie }} · {{ ruta.tipo }} · {{ ruta.protegidaPorRuta ? 'guard de ruta declarado' : 'sin guard de ruta' }}</span>
                @if (ruta.rutaNavegable) {
                  <a [routerLink]="ruta.rutaNavegable" [attr.aria-label]="'Abrir ruta declarada ' + ruta.path">Abrir ruta</a>
                } @else {
                  <span class="pendiente">Sin vínculo directo: requiere contexto, parámetro o redirección</span>
                }
              </li>
            }
          </ol>
        </section>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface,#17212d); }
    .inventario { box-sizing:border-box; max-width:78rem; margin:auto; padding:clamp(1rem,3vw,2.5rem); }
    .etiqueta { font-weight:700; color:var(--mat-sys-primary,#2157a3); }
    h1 { font-size:clamp(1.5rem,3vw,2.1rem); line-height:1.2; }
    h2 { font-size:1.2rem; margin-top:1.7rem; }
    p { line-height:1.55; overflow-wrap:anywhere; }
    .programas,.rutas { padding-left:1.4rem; display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:.6rem 1rem; }
    li { min-width:0; overflow-wrap:anywhere; padding:.7rem; border:1px solid var(--mat-sys-outline-variant,#aeb8c1); border-radius:.5rem; }
    .ruta { display:block; font-family:monospace; overflow-wrap:anywhere; }
    .meta,.pendiente { display:block; font-size:.87rem; margin:.35rem 0; }
    .pendiente { color:var(--mat-sys-on-surface-variant,#475569); }
    a { box-sizing:border-box; display:inline-block; min-height:44px; padding:.65rem .8rem; border:1px solid currentColor; border-radius:.4rem; text-decoration:underline; color:inherit; }
    a:focus-visible { outline:3px solid var(--mat-sys-primary,#2157a3); outline-offset:3px; }
    @media(max-width:48rem) { .programas,.rutas { grid-template-columns:1fr; } }
  `]
})
export class C1304InventarioGlobalComponent {
  private readonly router = inject(Router);
  private readonly plataforma = inject(PlataformaSesionService);
  readonly programas = PROGRAMAS_PLAN_V19;
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly rutasVisibles = computed(() => this.autorizado() ? inventariarRutasV19(this.router.config) : []);
  readonly lazyPendiente = computed(() => this.rutasVisibles().filter(r => r.hijosLazySinResolver).length);
}
