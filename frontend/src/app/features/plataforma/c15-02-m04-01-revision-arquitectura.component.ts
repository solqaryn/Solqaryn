import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

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
    <section class="review" aria-labelledby="review-title" data-testid="c1502-review">
      <header>
        <p class="eyebrow">SOLQARYN · DEV · arquitectura</p>
        <h1 id="review-title">Revisión de arquitectura e integraciones</h1>
        <p>Inventario de evidencias requeridas, no comprobaciones productivas ejecutadas.</p>
        <a routerLink="/plataforma" aria-label="Volver a plataforma">Volver a plataforma</a>
      </header>
      @if (autorizado()) {
        <section aria-labelledby="scope-title">
          <h2 id="scope-title">Superficies por contrastar</h2>
          <ul class="cards">
            @for (item of superficies; track item.id) {
              <li data-testid="architecture-item">
                <h3>{{ item.nombre }}</h3>
                <p>{{ item.objetivo }}</p>
                <p class="pending"><strong>NO_VERIFICADO</strong> · sin acceso a proveedores reales</p>
              </li>
            }
          </ul>
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
    h1 { font-size:clamp(1.5rem,3.5vw,2.25rem); line-height:1.2; }
    h2 { margin-top:1.5rem; }
    h3 { margin:.1rem 0 .5rem; font-size:1.08rem; }
    p,li,aside { line-height:1.55; overflow-wrap:anywhere; }
    a { display:inline-flex; min-height:44px; align-items:center; padding:.5rem .8rem; border:2px solid currentColor; border-radius:.5rem; }
    a:focus-visible { outline:3px solid var(--mat-sys-primary,#125b8a); outline-offset:3px; }
    .cards { display:grid; grid-template-columns:repeat(auto-fit,minmax(min(100%,16rem),1fr)); gap:.85rem; padding:0; list-style:none; }
    .cards li { min-width:0; padding:1rem; border:1px solid var(--mat-sys-outline,#76818a); border-radius:.7rem; }
    .pending { border-inline-start:3px solid var(--mat-sys-outline,#76818a); padding-inline-start:.65rem; }
    aside { margin-top:1.4rem; padding:.85rem; border:1px solid var(--mat-sys-outline,#76818a); border-radius:.5rem; }
    @media (max-width:480px) { .review { width:calc(100% - 1.5rem); } }
  `]
})
export class C1502RevisionArquitecturaComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly superficies = [
    { id: 'hosting', nombre: 'Hosting y ejecución', objetivo: 'Contrastar topología, versionado y rollback sin activar despliegues.' },
    { id: 'cdn', nombre: 'Cloudflare y DNS', objetivo: 'Revisar rutas, registros y límites sin modificar dominios.' },
    { id: 'objects', nombre: 'R2 y archivos', objetivo: 'Examinar contratos de acceso sin credenciales ni copias reales.' },
    { id: 'database', nombre: 'MySQL y datos', objetivo: 'Identificar migraciones, integridad y restauración pendientes de pruebas.' },
    { id: 'providers', nombre: 'Proveedores e integración', objetivo: 'Validar dependencias, fallos seguros y evidencias antes de conectar servicios.' }
  ] as const;
}
