import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C14-02.M04.01: guía privada de amenazas; nunca concede permisos ni ejecuta pentest. */
@Component({
  selector: 'app-revision-amenazas-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="threat-guide" aria-labelledby="threat-title">
      <header>
        <p class="eyebrow">SOLQARYN · Evaluación de amenazas</p>
        <h1 id="threat-title">Verificaciones de acceso y abuso</h1>
        <p>Guía privada sin peticiones a registros, proveedores ni datos productivos.</p>
        <a routerLink="/plataforma" aria-label="Volver a administración de plataforma">Volver a plataforma</a>
      </header>
      @if (accesoVerificado()) {
        <section class="notice" aria-label="Límites de ejecución">
          <strong>Solo consulta · no realiza pentest ni modifica políticas</strong>
          <p>Las mitigaciones requieren revisión y pruebas reales del backend.</p>
        </section>
        <section aria-labelledby="amenazas-title">
          <h2 id="amenazas-title">Cuatro superficies críticas a comprobar</h2>
          <ol class="threat-list">
            <li><h3>BOLA/IDOR y pertenencia</h3><p>Rechazar identificadores de otro tenant, sucursal, dueño o rol, aun si son válidos sintácticamente.</p></li>
            <li><h3>Suplantación y sesión</h3><p>Validar identidad servidor, MFA, revocación, reenvíos y denegaciones ante tokens obsoletos.</p></li>
            <li><h3>Archivos, jobs y caché</h3><p>Aislar rutas, colas, recibos, firmas, claves y errores sin enumeración ni filtración.</p></li>
            <li><h3>Host, backup y recuperación</h3><p>Exigir validación del host, precondiciones de restore, rollback y ensayos aislados antes de actuar.</p></li>
          </ol>
        </section>
        <p role="note">Pruebas HTTP JWT, tenant A/B y pentest en vivo: NO_MEDIDO. QA, main y PROD no autorizados.</p>
      } @else {
        <p class="notice" role="alert">Acceso de plataforma no confirmado. Guía privada oculta.</p>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface, #17212d); }
    .threat-guide { box-sizing:border-box; max-width:70rem; margin:auto; padding:clamp(1rem,3vw,2.5rem); }
    h1 { line-height:1.2; font-size:clamp(1.6rem,4vw,2.35rem); }
    h2 { font-size:1.3rem; margin-top:1.5rem; }
    h3 { font-size:1rem; margin:0 0 .35rem; }
    p { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { color:var(--mat-sys-primary, #2157a3); font-weight:700; }
    header a { display:inline-block; border:1px solid currentColor; border-radius:.55rem; padding:.7rem .85rem; color:inherit; text-decoration:underline; }
    header a:focus-visible { outline:3px solid var(--mat-sys-primary, #2157a3); outline-offset:3px; }
    .notice { border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; padding:1rem; margin:1rem 0; }
    .threat-list { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:.85rem; padding:0; list-style:none; }
    .threat-list li { min-width:0; border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; padding:.9rem; }
    @media(max-width:45rem) { .threat-list { grid-template-columns:1fr; } }
  `]
})
export class RevisionAmenazasV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly accesoVerificado = computed(() => {
    const s = this.plataforma.sesion();
    return s?.ambito === 'Plataforma' && s.esAdministrador === true;
  });
}
