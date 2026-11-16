import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C15-03.M04.01: superficie de guia privada; nunca ejecuta pentest ni otorga permisos. */
@Component({
  selector: 'app-auditoria-tenancy-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="tenancy-audit" aria-labelledby="tenancy-audit-title">
      <header>
        <p class="eyebrow">SOLQARYN · Auditoría de seguridad y tenancy</p>
        <h1 id="tenancy-audit-title">Fronteras de aislamiento y evidencia</h1>
        <p>Guía privada, sin consultas de datos ni pruebas de seguridad en vivo.</p>
        <a routerLink="/plataforma" aria-label="Regresar a administración de plataforma">Volver a plataforma</a>
      </header>
      @if (accesoVerificado()) {
        <section class="notice" aria-label="Alcance de solo lectura">
          <strong>Solo lectura · ninguna prueba o autorización ejecutada</strong>
          <p>Los criterios siguientes son un contrato de revisión futura, no resultados certificados.</p>
        </section>
        <section aria-labelledby="fronteras-title">
          <h2 id="fronteras-title">Cuatro frentes de comprobación futura</h2>
          <ol class="fronteras" aria-label="Matriz de aislamiento requerida">
            <li><h3>Tenant A/B y sucursal</h3><p>Rechazar cruce de EmpresaId, SucursalId, consultas, índices y exportaciones, incluso con identificadores repetidos.</p></li>
            <li><h3>Propiedad y pertenencia</h3><p>Revalidar usuario, rol, membresía, revocación y denegación por defecto en backend.</p></li>
            <li><h3>Archivos, almacenamiento y jobs</h3><p>Verificar ownership, rutas, claves, idempotencia y doble ejecución; ningún restore o proveedor se invoca aquí.</p></li>
            <li><h3>Control plane y secretos</h3><p>Probar fallos seguros, límites, registros mínimos y ausencia de credenciales en respuestas; no conceder privilegios.</p></li>
          </ol>
        </section>
        <p class="limites" role="note">Estado de pentest, resultados A/B y métricas: NO_MEDIDO. QA/PROD no autorizados.</p>
      } @else {
        <p class="denied" role="alert">Sesión de plataforma no confirmada. Guía protegida oculta.</p>
      }
    </main>
  `,
  styles: [`
    :host { display: block; color: var(--mat-sys-on-surface, #17212d); }
    .tenancy-audit { max-width: 72rem; box-sizing: border-box; margin: auto; padding: clamp(1rem,3vw,2.5rem); }
    h1 { line-height: 1.2; font-size: clamp(1.6rem,4vw,2.3rem); }
    h2 { margin-top: 1.5rem; font-size: 1.3rem; }
    h3 { font-size: 1rem; margin: 0 0 .3rem; }
    p { line-height: 1.55; overflow-wrap: anywhere; }
    .eyebrow { font-weight: 700; color: var(--mat-sys-primary, #2157a3); }
    header a { display: inline-block; border: 1px solid currentColor; border-radius: .55rem; padding: .7rem .9rem; color: inherit; text-decoration: none; }
    header a:focus-visible { outline: 3px solid var(--mat-sys-primary, #2157a3); outline-offset: 3px; }
    .notice,.denied { border: 1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius: .75rem; padding: 1rem; margin-top: 1rem; }
    .notice { background: var(--mat-sys-surface-container-low, #f5f7fa); }
    .fronteras { display: grid; grid-template-columns: repeat(2,minmax(0,1fr)); gap: .85rem; list-style: none; padding: 0; }
    .fronteras li { min-width: 0; border: 1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius: .75rem; padding: 1rem; }
    .fronteras p { margin: 0; }
    .limites { font-weight: 600; }
    @media(max-width: 45rem) { .fronteras { grid-template-columns: 1fr; } }
  `]
})
export class AuditoriaTenancyV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  /** El navegador no crea autoridad: solo expone el grant actualmente confirmado por servidor. */
  readonly accesoVerificado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
}
