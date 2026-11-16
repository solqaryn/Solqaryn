import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C11-04.M04.01: observabilidad UI privada y estática, sin consultas ni exporter. */
@Component({
  selector: 'app-observabilidad-privada-v5',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="observability-guide" aria-labelledby="obs-title">
      <header>
        <p class="eyebrow">SOLQARYN · Observabilidad privada</p>
        <h1 id="obs-title">Diagnóstico y señales seguras</h1>
        <p>Esta guía describe requisitos de evidencia; no lee métricas ni inicia un worker.</p>
        <a routerLink="/plataforma" aria-label="Regresar a plataforma">Volver a plataforma</a>
      </header>
      @if (verificado()) {
        <section class="notice" aria-label="Límites de medición">
          <strong>Solo lectura · sin SLO ni costos verificados</strong>
          <p>Probar las fuentes reales en un entorno aislado antes de publicar alertas o atribuir cumplimiento.</p>
        </section>
        <section aria-labelledby="obs-checks">
          <h2 id="obs-checks">Cuatro frentes de observabilidad</h2>
          <ol class="obs-points">
            <li><h3>Logs y confidencialidad</h3><p>Permitir sólo códigos estructurados, sin JWT, actor, tenant ni rutas privadas.</p></li>
            <li><h3>Métricas y costos</h3><p>Validar etiquetas de baja cardinalidad, presupuesto y deduplicación antes del exporter.</p></li>
            <li><h3>Trazas y latencia</h3><p>Conservar contexto de autoridad servidor, límites, correlación segura y muestreo acotado.</p></li>
            <li><h3>Alertas y diagnóstico</h3><p>Comprobar revocación, errores, reintentos, falso positivo y rollback sin divulgar secretos.</p></li>
          </ol>
        </section>
        <p role="note">SLO, RTO/RPO, costos, latencia y proveedor de telemetría: NO_MEDIDO. Sin autorizaciones QA/main/PROD.</p>
      } @else {
        <p class="notice" role="alert">Sesión de plataforma no confirmada. Observabilidad privada oculta.</p>
      }
    </main>
  `,
  styles: [`
    :host { display:block; color:var(--mat-sys-on-surface, #17212d); }
    .observability-guide { box-sizing:border-box; max-width:70rem; margin:auto; padding:clamp(1rem,3vw,2.5rem); }
    h1 { line-height:1.2; font-size:clamp(1.6rem,4vw,2.3rem); }
    h2 { font-size:1.3rem; margin-top:1.5rem; }
    h3 { font-size:1rem; margin:0 0 .35rem; }
    p { line-height:1.55; overflow-wrap:anywhere; }
    .eyebrow { font-weight:700; color:var(--mat-sys-primary, #2157a3); }
    header a { display:inline-block; border:1px solid currentColor; border-radius:.55rem; padding:.7rem .9rem; color:inherit; text-decoration:underline; }
    header a:focus-visible { outline:3px solid var(--mat-sys-primary, #2157a3); outline-offset:3px; }
    .notice { border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; padding:.9rem; margin:1rem 0; }
    .obs-points { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); list-style:none; padding:0; gap:.85rem; }
    .obs-points li { border:1px solid var(--mat-sys-outline-variant, #aeb8c1); border-radius:.7rem; min-width:0; padding:1rem; }
    @media(max-width:45rem) { .obs-points { grid-template-columns:1fr; } }
  `]
})
export class ObservabilidadPrivadaV5Component {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly verificado = computed(() => {
    const session = this.plataforma.sesion();
    return session?.ambito === 'Plataforma' && session.esAdministrador === true;
  });
}
