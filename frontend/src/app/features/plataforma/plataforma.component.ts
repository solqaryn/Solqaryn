import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { DirectorioPlataforma, PlataformaEmpresasService } from '../../core/auth/plataforma-empresas.service';
import { PlataformaGestionMvpComponent } from './plataforma-gestion-mvp.component';
import { DesignSystemV5Component } from './design-system-v5.component';
import { C1404ReleaseBrandingV17Component } from './c14-04-m04-03-release-branding.component';

@Component({
  selector: 'app-plataforma',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, PlataformaGestionMvpComponent, DesignSystemV5Component, C1404ReleaseBrandingV17Component],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="plataforma" aria-labelledby="plataforma-title">
      <header class="heading">
        <div class="heading__text">
          <span class="eyebrow"><mat-icon aria-hidden="true">verified_user</mat-icon> SOLQARYN · Plataforma</span>
          <h1 id="plataforma-title">{{ vistaFlujoV5 ? 'Ciclo V5 · estados y controles' : vistaCoberturaV5 ? 'Cobertura V5 · trazabilidad' : 'Administración global' }}</h1>
          <p>{{ vistaFlujoV5 ? 'Consulta del contrato de ejecución, sin acceso a datos ni acciones operativas.' : vistaCoberturaV5 ? 'Explora las relaciones entre requisito, trabajo y evidencia sin exponer datos empresariales.' : 'Acceso independiente de una empresa. La autorización se comprueba directamente en el servidor.' }}</p>
        </div>
        <div class="heading__actions">
          <button mat-stroked-button type="button" (click)="revalidar()" [disabled]="validando()">
            @if (validando()) { <mat-spinner diameter="18" aria-label="Verificando autoridad"></mat-spinner> }
            @else { <mat-icon>refresh</mat-icon> }
            Revalidar sesión
          </button>
          @if (vistaFlujoV5 || vistaCoberturaV5) {
            <a mat-stroked-button routerLink="/plataforma"><mat-icon aria-hidden="true">arrow_back</mat-icon> Administración</a>
          } @else {
            <a mat-stroked-button routerLink="/plataforma/flujo-v5"><mat-icon aria-hidden="true">account_tree</mat-icon> Consultar ciclo V5</a>
            <a mat-stroked-button routerLink="/plataforma/auditoria-ux-v5"><mat-icon aria-hidden="true">accessibility_new</mat-icon> Auditoría UX</a>
            <a mat-stroked-button routerLink="/plataforma/cobertura-v5"><mat-icon aria-hidden="true">rule</mat-icon> Cobertura V5</a>
          }
          <a mat-button routerLink="/login"><mat-icon>apartment</mat-icon> Acceder a una empresa</a>
        </div>
      </header>

      @if (vistaFlujoV5) {
        <div class="v5-feedback" aria-live="polite" aria-atomic="true" data-testid="release-readiness-feedback"
             [attr.data-state]="validando() ? 'cargando' : permisoReadinessDenegado() ? 'sin_permiso' : error() ? 'error' : !sesion() ? 'sin_permiso' : 'exito'">
          @if (validando()) {
            <p role="status" data-testid="release-loading"><mat-spinner diameter="20" aria-label="Comprobando autoridad"></mat-spinner> Verificando permiso de plataforma…</p>
          } @else if (permisoReadinessDenegado()) {
            <p role="alert" data-testid="release-denied">La sesión no tiene permisos de plataforma vigentes. No se muestran comprobaciones privadas.</p>
          } @else if (error()) {
            <p role="alert" data-testid="release-error" class="v5-feedback-error">No se pudo verificar la sesión. No hay acceso operativo y puede reintentarse la validación.</p>
          } @else if (!sesion()) {
            <p role="alert" data-testid="release-denied">Permisos de plataforma no confirmados. La consulta V5 está restringida.</p>
          } @else {
            <p role="status" data-testid="release-success">Sesión de plataforma verificada para consulta de solo lectura.</p>
            <p data-testid="release-empty" data-evidence-state="vacio">Sin resultados institucionales conectados: esta vista no lee microtareas ni certifica estados.</p>
          }
        </div>
      }


      @if (vistaCoberturaV5) {
        <div class="v5-feedback" aria-live="polite" aria-atomic="true">
          @if (validando()) {
            <p role="status"><mat-spinner diameter="20" aria-label="Revalidando permiso de cobertura"></mat-spinner> Comprobando permiso de cobertura…</p>
          } @else if (error()) {
            <p role="alert" class="v5-feedback-error">La sesión global dejó de estar autorizada o no pudo verificarse. No se permite ninguna operación.</p>
          } @else if (!sesion()) {
            <p role="alert">Sin permiso de plataforma confirmado. La cobertura permanece restringida.</p>
          } @else {
            <p role="status">Permiso de consulta confirmado. La cobertura sigue en modo de solo lectura.</p>
          }
        </div>
      }

      @if (sesion(); as actual) {
        <mat-card class="authority-card">
          <mat-card-header>
            <mat-icon mat-card-avatar aria-hidden="true">admin_panel_settings</mat-icon>
            <mat-card-title>Identidad de plataforma verificada</mat-card-title>
            <mat-card-subtitle>Confirmada mediante JWT vigente y autoridad MySQL, no mediante permisos empresariales</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <dl class="claims">
              <div><dt>Cuenta</dt><dd>{{ actual.nombreUsuario }}</dd></div>
              <div><dt>Rol</dt><dd>{{ actual.rolNombre }}</dd></div>
              <div><dt>Ámbito</dt><dd>{{ actual.ambito }}</dd></div>
            </dl>
          </mat-card-content>
        </mat-card>

        @if (vistaFlujoV5) {
          <!-- C14-04.M04.03: presets cosméticos autorizados; sin go-live. -->
          <app-c1404-release-branding-v17 />
          <!-- C14-04.M04.01: superficie read-only en flujo privado ya autorizado. -->
          <mat-card class="release-readiness-card" aria-labelledby="release-readiness-title" data-testid="release-readiness-contract">
            <mat-card-content>
              <h2 id="release-readiness-title">Preparación de salida · evidencia pendiente</h2>
              <p>Contrato de comprobación para DEV. Esta pantalla no consulta CI, DNS, respaldo ni métricas de operación; no representa una aprobación de despliegue.</p>
              <ol class="release-readiness-criteria" aria-label="Puertas de preparación no certificadas">
                <li><strong>Smoke funcional y aislamiento:</strong> <span>SIN_VERIFICAR</span> · requieren pruebas de acceso y rechazo por ámbito.</li>
                <li><strong>Soporte y responsables:</strong> <span>SIN_VERIFICAR</span> · responsables y respuesta a incidentes por acreditar.</li>
                <li><strong>Alertas y ventana de observación:</strong> <span>NO_MEDIDO</span> · sin SLO ni telemetría real conectada.</li>
                <li><strong>Backup, restauración y reversión:</strong> <span>SIN_VERIFICAR</span> · ensayo aislado y autorización independiente pendientes.</li>
              </ol>
              <p role="note">Ninguna puerta autoriza QA, main, PROD, DNS, R2, restore ni cambios de infraestructura. La autoridad permanece en backend y en evidencias verificadas.</p>
            </mat-card-content>
          </mat-card>
          <mat-card class="v5-card" aria-labelledby="v5-title">
            <mat-card-content>
              <div class="v5-overview">
                <div>
                  <p class="v5-kicker">Modelo de trabajo · solo lectura</p>
                  <h2 id="v5-title">De la solicitud a la evidencia DEV</h2>
                  <p>Esta vista explica las puertas del contrato. No consulta PLAN_MAESTRO, COLA ni BITACORA y no refleja el estado actual de ninguna microtarea.</p>
                </div>
                <span class="v5-readonly"><mat-icon aria-hidden="true">lock_outline</mat-icon> Sin operaciones</span>
              </div>
              <ol class="v5-steps" aria-label="Secuencia contractual de estados">
                <li><span class="v5-step-index" aria-hidden="true">01</span><div><strong>PENDIENTE</strong><p>El ID existe en el plan; dependencias y DAG deben autorizar el inicio. COLA no concede permisos por sí sola.</p></div></li>
                <li><span class="v5-step-index" aria-hidden="true">02</span><div><strong>EN_PROGRESO</strong><p>Una Task autorizada conserva la propiedad exclusiva. Sin candidato completo no puede declarar VALIDANDO.</p></div></li>
                <li><span class="v5-step-index" aria-hidden="true">03</span><div><strong>VALIDANDO</strong><p>Auditorías A0–A3 y pruebas pertinentes; ninguna ejecución pendiente, cancelada o omitida equivale a PASS.</p></div></li>
                <li><span class="v5-step-index" aria-hidden="true">04</span><div><strong>LISTO_DEV</strong><p>Solo tras A0–A4, SHA exacto, Scope Lock y readback. A5 queda a cargo de una Task distinta.</p></div></li>
              </ol>
              <p class="v5-boundary" role="note"><mat-icon aria-hidden="true">shield</mat-icon>
                Ninguna transición, autorización JWT/RBAC, escritura entre empresas ni promoción a QA o producción se realiza desde esta pantalla.
              </p>
            </mat-card-content>
          </mat-card>

        } @else if (vistaCoberturaV5) {
          <mat-card class="v5-card" [class.v5-coverage-contrast]="contrasteCobertura()" aria-labelledby="cobertura-v5-title">
            <mat-card-content>
              <div class="v5-overview">
                <div>
                  <p class="v5-kicker">Cobertura integrada · superficie de consulta</p>
                  <h2 id="cobertura-v5-title">Cadena de trazabilidad</h2>
                  <p>El contrato conecta requisitos, microtareas, pruebas y evidencia. No hay datos de cobertura en vivo conectados a esta pantalla.</p>
                </div>
                <span class="v5-readonly"><mat-icon aria-hidden="true">lock_outline</mat-icon> Sin escritura</span>
              </div>
              <div class="v5-coverage-branding" role="group" aria-label="Presentación visual de la matriz">
                <span class="v5-brand-mark"><mat-icon aria-hidden="true">verified</mat-icon> Identidad SOLQARYN</span>
                <button mat-stroked-button type="button" (click)="contrasteCobertura.update(value => !value)"
                        [attr.aria-pressed]="contrasteCobertura()" aria-label="Alto contraste de cobertura">
                  <mat-icon aria-hidden="true">contrast</mat-icon> Alto contraste
                </button>
              </div>
              <div class="v5-coverage-scroll" role="region" aria-label="Columnas del contrato de cobertura" tabindex="0">
                <table class="v5-coverage">
                  <thead>
                    <tr><th scope="col">Requisito</th><th scope="col">Microtarea</th><th scope="col">Contrato</th><th scope="col">Prueba</th><th scope="col">Evidencia</th></tr>
                  </thead>
                  <tbody>
                    <tr><td colspan="5" class="v5-empty">Sin datos de cobertura verificados. La matriz institucional debe leerse en su fuente autorizada antes de registrar un resultado.</td></tr>
                  </tbody>
                </table>
              </div>
              <p class="v5-boundary" role="note"><mat-icon aria-hidden="true">shield</mat-icon>
                No se infieren PASS, propietarios ni SHAs. Esta vista no consulta ni modifica PLAN_MAESTRO, COLA o BITACORA y no emite operaciones ni eventos.
              </p>
            </mat-card-content>
          </mat-card>
        } @else {
        <div class="modules" aria-label="Capacidades de la plataforma">
          <mat-card class="module-card">
            <mat-card-content>
              <mat-icon aria-hidden="true">domain</mat-icon>
              <h2>Gestión de empresas</h2>
              <p>Consulta del directorio global, incluida la identidad de empresas inactivas, sin acceso a registros comerciales.</p>
              <button mat-stroked-button type="button" (click)="cargarEmpresas()" [disabled]="cargandoEmpresas()">Actualizar directorio</button>
              @if (directorio(); as listado) {
                @if (listado.puedeCrear) {
                  <p class="available">Alta autorizable por el backend; la operación requiere flujo de aprobación.</p>
                } @else {
                  <p class="pending">Alta desactivada por los controles de seguridad del servidor</p>
                }
              } @else {
                <p class="pending">La creación de empresas permanece bloqueada por defecto</p>
              }
            </mat-card-content>
          </mat-card>
          <mat-card class="module-card">
            <mat-card-content>
              <mat-icon aria-hidden="true">security</mat-icon>
              <h2>Seguridad y trazabilidad</h2>
              <p>Cada comprobación de acceso global es auditada por el backend. Los administradores empresariales no reciben privilegios de plataforma.</p>
              <span class="available">Control de sesión activo</span>
            </mat-card-content>
          </mat-card>
        </div>
        <details class="design-system-preview">
          <summary>Biblioteca visual · tokens y componentes</summary>
          <app-design-system-v5 />
        </details>
        <details class="ui-lightness-v5" aria-label="Contrato de ligereza de interfaz">
          <summary>Ligereza y percepción · revisión de interfaz</summary>
          <section class="ui-lightness-v5__content" aria-labelledby="ui-lightness-v5-title">
            <div class="ui-lightness-v5__heading">
              <div>
                <h2 id="ui-lightness-v5-title">Presupuesto de ligereza visual</h2>
                <p>Contrato de revisión para pantallas ERP. Los umbrales son objetivos, no mediciones obtenidas del dispositivo.</p>
              </div>
              <button mat-stroked-button type="button" class="ui-lightness-v5__toggle"
                [attr.aria-pressed]="ligerezaCompacta()"
                (click)="ligerezaCompacta.update(actual => !actual)">
                {{ ligerezaCompacta() ? 'Ver criterios' : 'Reducir detalles' }}
              </button>
            </div>
        <div class="ui-lightness-v5__feedback" aria-live="polite" aria-atomic="true" data-testid="c0504-feedback">
          @if (cargandoEmpresas()) {
            <p role="status" data-testid="c0504-loading">Cargando datos autorizados del directorio…</p>
          } @else if (errorEmpresas()) {
            @if (directorioSinPermiso()) {
              <p role="alert" data-testid="c0504-permission">Permiso insuficiente. No se muestran datos de empresas ni detalles del servidor.</p>
            } @else {
              <p role="alert" data-testid="c0504-error">No se pudo actualizar el directorio. Puede reintentarse sin perder los controles de acceso.</p>
              <button mat-stroked-button type="button" [disabled]="cargandoEmpresas()" (click)="cargarEmpresas()">Reintentar directorio</button>
            }
          } @else if (directorio(); as resultado) {
            @if (resultado.items.length === 0) {
              <p role="status" data-testid="c0504-empty">Consulta completada: no hay empresas que mostrar.</p>
            } @else {
              <p role="status" data-testid="c0504-success">Consulta completada: {{ resultado.items.length }} empresas en esta página autorizada.</p>
            }
          } @else {
            <p role="status" data-testid="c0504-pending">Esperando resultado de consulta autorizada.</p>
          }
        </div>
            <div class="ui-lightness-v5__branding" role="group"
              aria-label="Marca visual autorizada" data-testid="c0504-branding"
              [class.ui-lightness-v5__branding--contrast]="marcaLigereza() === 'contraste'">
              <strong>Marca visual permitida</strong>
              <div class="ui-lightness-v5__branding-actions">
                @for (opcion of opcionesMarcaLigereza; track opcion.id) {
                  <button mat-stroked-button type="button"
                    [attr.data-testid]="'c0504-brand-' + opcion.id"
                    [attr.aria-pressed]="marcaLigereza() === opcion.id"
                    (click)="cambiarMarcaLigereza(opcion.id)">{{ opcion.nombre }}</button>
                }
              </div>
              <p role="status" data-testid="c0504-branding-state">
                {{ marcaLigereza() === 'contraste' ? 'Contraste reforzado seleccionado' : 'Institucional seleccionado' }}
              </p>
            </div>
            @if (ligerezaCompacta()) {
              <p class="ui-lightness-v5__status" role="status">Resumen compacto: tres criterios disponibles sin solicitudes adicionales.</p>
            } @else {
              <ul class="ui-lightness-v5__checks" aria-label="Criterios de ligereza">
                <li><strong>Carga inicial</strong><span>Verificar el presupuesto de bundle en la compilación; tamaño real: no medido aquí.</span></li>
                <li><strong>Respuesta visual</strong><span>Objetivos: LCP ≤ 2,5 s y CLS ≤ 0,1; sin resultados de campo certificados.</span></li>
                <li><strong>Operaciones</strong><span>Evitar duplicar llamadas y mantener navegación y lectura sin nuevas escrituras.</span></li>
              </ul>
            }
            <p class="ui-lightness-v5__boundary" role="note">Consulta local sin configuraciones persistentes, permisos adicionales ni acceso a otras empresas.</p>
          </section>
        </details>
        <mat-card class="directory-card" aria-label="Directorio de empresas">
          <mat-card-header>
            <mat-card-title>Directorio global de empresas</mat-card-title>
            <mat-card-subtitle>Solo metadatos de administración, sin información comercial de tenants</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            @if (cargandoEmpresas()) {
              <div class="directory-loading"><mat-spinner diameter="24" aria-label="Cargando directorio"></mat-spinner> Consultando empresas…</div>
            } @else if (errorEmpresas()) {
              <p role="alert" class="directory-error">{{ errorEmpresas() }}</p>
              <button mat-stroked-button type="button" (click)="cargarEmpresas()">Reintentar</button>
            } @else if (directorio(); as resultado) {
              <p class="directory-summary">Total de empresas: {{ resultado.total }}</p>
              @if (!resultado.items.length) {
                <p>No hay empresas para mostrar.</p>
              } @else {
                <div class="directory-scroll">
                  <table aria-label="Empresas registradas">
                    <thead><tr><th scope="col">Empresa</th><th scope="col">Estado</th><th scope="col">Usuarios activos</th></tr></thead>
                    <tbody>
                      @for (empresa of resultado.items; track empresa.id) {
                        <tr>
                          <td>{{ empresa.nombre }}</td>
                          <td>{{ empresa.activa ? 'Activa' : 'Inactiva' }}</td>
                          <td>{{ empresa.usuariosActivos }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }
              @if (resultado.total > resultado.tamano) {
                <div class="pagination">
                  <button mat-stroked-button type="button" [disabled]="resultado.pagina <= 1" (click)="cargarEmpresas(resultado.pagina - 1)">Anterior</button>
                  <span>Página {{ resultado.pagina }} · {{ resultado.total }} empresas</span>
                  <button mat-stroked-button type="button" [disabled]="resultado.pagina * resultado.tamano >= resultado.total" (click)="cargarEmpresas(resultado.pagina + 1)">Siguiente</button>
                </div>
              }
            }
          </mat-card-content>
        </mat-card>
        <app-plataforma-gestion-mvp [directorio]="directorio()" (actualizarDirectorio)="cargarEmpresas()"></app-plataforma-gestion-mvp>
        }
      } @else if (!validando() && !vistaCoberturaV5) {
        <mat-card class="denied">
          <mat-card-content>
            <mat-icon aria-hidden="true">gpp_bad</mat-icon>
            <h2>Sin autoridad global verificable</h2>
            <p role="alert">{{ error() || 'No se recibió una sesión de plataforma válida.' }}</p>
            <a mat-stroked-button routerLink="/login">Regresar al acceso</a>
          </mat-card-content>
        </mat-card>
      }
    </section>
  `,
  styles: [`
    :host { display: block; }
    .plataforma { max-width: 1120px; margin: 0 auto; padding: clamp(16px, 3vw, 36px); }
    .heading { display: flex; align-items: flex-end; justify-content: space-between; flex-wrap: wrap; gap: 20px; margin-bottom: 24px; }
    .heading__text { max-width: 680px; min-width: 0; }
    .eyebrow { display: inline-flex; align-items: center; gap: 8px; font-size: .8rem; font-weight: 750; letter-spacing: .08em; text-transform: uppercase; color: var(--color-primary); }
    .eyebrow mat-icon { font-size: 20px; width: 20px; height: 20px; }
    h1 { margin: 10px 0; font-size: clamp(1.8rem, 3.5vw, 2.8rem); line-height: 1.15; overflow-wrap: anywhere; }
    p { line-height: 1.6; }
    .heading p { margin: 0; color: var(--color-text-muted); }
    .heading__actions { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; }
    .heading__actions button, .heading__actions a { min-height: 44px; }
    .authority-card { margin-bottom: 20px; }
    .claims { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; margin: 24px 0 4px; }
    .claims div { border-radius: 12px; padding: 16px; background: var(--color-background, #f8fafc); min-width: 0; }
    dt { color: var(--color-text-muted); font-size: .85rem; }
    dd { margin: 6px 0 0; font-weight: 700; overflow-wrap: anywhere; }
    .modules { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px; }
    .module-card { height: 100%; }
    .module-card mat-card-content { padding: 24px; }
    .module-card mat-icon { color: var(--color-primary); font-size: 30px; width: 30px; height: 30px; }
    h2 { margin: 12px 0 8px; font-size: 1.15rem; }
    .module-card p { color: var(--color-text-muted); }
    .pending, .available { display: inline-block; border: 1px solid currentColor; border-radius: 999px; padding: 5px 11px; font-weight: 650; font-size: .8rem; }
    .pending { color: #955500; }
    .available { color: #22693d; }
    .v5-card mat-card-content { padding: clamp(18px, 3vw, 32px); }
    .v5-feedback { margin: 0 0 18px; padding: 14px 18px; border: 1px solid #dfe5ec; border-radius: 12px; }
    .v5-feedback p { margin: 4px 0; }
    .v5-feedback p:has(mat-spinner) { display: flex; align-items: center; gap: 10px; }
    .v5-feedback-error { color: var(--color-danger, #b3261e); }
    .v5-overview { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; flex-wrap: wrap; }
    .v5-overview > div { max-width: 650px; min-width: 0; overflow-wrap: anywhere; }
    .v5-kicker { font-size: .78rem; letter-spacing: .08em; text-transform: uppercase; font-weight: 750; color: var(--color-primary); }
    .v5-readonly { display: inline-flex; align-items: center; gap: 5px; font-weight: 700; font-size: .83rem; padding: 8px 12px; border-radius: 99px; background: var(--color-background, #f5f7fa); }
    .v5-readonly mat-icon { font-size: 18px; width: 18px; height: 18px; }
    .v5-steps { list-style: none; margin: 24px 0 0; padding: 0; display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; }
    .v5-steps li { display: flex; gap: 14px; min-width: 0; padding: 20px; border: 1px solid #dfe5ec; border-radius: 14px; }
    .v5-steps strong { display: block; font-weight: 750; letter-spacing: .02em; }
    .v5-steps p { margin: 7px 0 0; color: var(--color-text-muted); }
    .v5-step-index { font-weight: 800; font-variant-numeric: tabular-nums; color: var(--color-primary); }
    .v5-boundary { display: flex; align-items: flex-start; gap: 10px; margin: 24px 0 0; padding: 14px; border-radius: 10px; background: var(--color-background, #f5f7fa); font-size: .9rem; }
    .v5-boundary mat-icon { flex: 0 0 24px; }
    @media (max-width: 760px) { .v5-steps { grid-template-columns: 1fr; } }
    .v5-coverage-branding { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 12px; margin-top: 20px; }
    .v5-brand-mark { display: inline-flex; align-items: center; gap: 8px; font-weight: 700; color: var(--color-primary, #165da0); }
    .v5-brand-mark { display: inline-flex; align-items: center; flex-wrap: wrap; min-width: 0; max-width: 100%; overflow-wrap: anywhere; }
    .v5-brand-mark mat-icon { flex: 0 0 20px; width: 20px; height: 20px; font-size: 20px; }
    .v5-coverage-branding button { min-height: 44px; height: auto; min-width: 0; max-width: 100%; white-space: normal; overflow-wrap: anywhere; }
    .v5-coverage-contrast { background-color: #0b1220; color: #f8fafc; }
    .v5-coverage-contrast .v5-kicker, .v5-coverage-contrast .v5-brand-mark { color: #b7e2ff; }
    .v5-coverage-contrast .v5-readonly { background-color: #334155; color: #f8fafc; }
    .v5-coverage-contrast .v5-coverage-scroll { border-color: #94a3b8; }
    .v5-coverage-contrast .v5-coverage th { background-color: #1e293b; color: #f8fafc; border-color: #64748b; }
    .v5-coverage-contrast .v5-coverage td { color: #f8fafc; border-color: #64748b; }
    .v5-coverage-contrast .v5-coverage .v5-empty { color: #e2e8f0; }
    .v5-coverage-contrast .v5-boundary { background-color: #1e293b; color: #f8fafc; }
    .v5-coverage-contrast .v5-coverage-branding button { color: #f8fafc; border-color: #94a3b8; }
    .v5-coverage-scroll { max-width: 100%; overflow: auto; margin-top: 24px; border: 1px solid #dfe5ec; border-radius: 12px; }
    .v5-coverage { min-width: 650px; width: 100%; border-collapse: collapse; }
    .v5-coverage th, .v5-coverage td { padding: 16px; text-align: left; border-bottom: 1px solid #dfe5ec; }
    .v5-coverage th { font-weight: 750; font-size: .85rem; background: var(--color-background, #f5f7fa); }
    .v5-coverage .v5-empty { color: var(--color-text-muted); line-height: 1.6; }
    .ui-lightness-v5 { margin-top: 20px; border: 1px solid #dfe5ec; border-radius: 12px; }
    .ui-lightness-v5 > summary { cursor: pointer; padding: 16px 20px; font-weight: 700; min-height: 44px; }
    .ui-lightness-v5 > summary:focus-visible { outline: 3px solid var(--color-primary, #165da0); outline-offset: 2px; }
    .ui-lightness-v5__content { padding: 8px clamp(16px, 3vw, 24px) 24px; }
    .ui-lightness-v5__heading { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; flex-wrap: wrap; }
    .ui-lightness-v5__heading p { max-width: 640px; color: var(--color-text-muted); }
    .ui-lightness-v5__toggle { min-height: 44px; }
    .ui-lightness-v5__toggle:focus-visible { outline: 3px solid var(--color-primary, #165da0); outline-offset: 2px; }
    .ui-lightness-v5__checks { margin: 16px 0; padding: 0; display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; list-style: none; }
    .ui-lightness-v5__checks li { min-width: 0; padding: 16px; border: 1px solid #dfe5ec; border-radius: 10px; overflow-wrap: anywhere; }
    .ui-lightness-v5__checks strong { display: block; margin-bottom: 8px; }
    .ui-lightness-v5__checks span, .ui-lightness-v5__boundary { color: var(--color-text-muted); }
    .ui-lightness-v5__boundary { margin: 12px 0 0; }
    .ui-lightness-v5__feedback { margin: 14px 0; padding: 12px 16px; border: 1px solid #dfe5ec; border-radius: 10px; background: var(--color-background, #f8fafc); overflow-wrap: anywhere; }
    .ui-lightness-v5__feedback p { margin: 4px 0 10px; }
    .ui-lightness-v5__feedback button { min-height: 44px; }
    .ui-lightness-v5__feedback [role='alert'] { color: var(--color-danger, #b3261e); }
    .ui-lightness-v5__branding { margin-top: 16px; padding: 16px; border: 1px solid #8da8bc; border-radius: 12px; background: #f4f7fb; color: #14293c; overflow-wrap: anywhere; }
    .ui-lightness-v5__branding-actions { display: flex; flex-wrap: wrap; gap: 12px; margin: 12px 0; }
    .ui-lightness-v5__branding-actions button { min-height: 44px; border-color: currentColor; color: inherit; }
    .ui-lightness-v5__branding-actions button:focus-visible { outline: 3px solid currentColor; outline-offset: 3px; }
    .ui-lightness-v5__branding p { margin: 8px 0 0; }
    .ui-lightness-v5__branding--contrast { background: #091422; border-color: #b8e2ff; color: #fff; }
    .ui-lightness-v5__branding--contrast .ui-lightness-v5__branding-actions button { border-color: #f8fafc; color: #fff; }
    @media (max-width: 375px) { .ui-lightness-v5__branding-actions { flex-direction: column; align-items: stretch; } }
    @media (max-width: 760px) { .ui-lightness-v5__checks { grid-template-columns: 1fr; } }
    .directory-card { margin-top: 20px; }
    .directory-card mat-card-content { padding: 20px 24px; }
    .directory-scroll { max-width: 100%; overflow-x: auto; }
    .directory-scroll table { width: 100%; border-collapse: collapse; }
    .directory-scroll th, .directory-scroll td { padding: 13px 12px; border-bottom: 1px solid #dfe5ec; text-align: left; }
    .directory-scroll th { font-size: .8rem; color: var(--color-text-muted); }
    .directory-loading { display: flex; align-items: center; gap: 12px; min-height: 52px; }
    .directory-error { color: var(--color-danger, #b3261e); }
    .directory-summary { color: var(--color-text-muted); }
    .pagination { display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 12px; margin-top: 16px; }
    .denied { max-width: 620px; }
    .denied mat-card-content { padding: 24px; }
    .denied mat-icon { color: var(--color-danger, #b3261e); }
    @media (max-width: 680px) { .claims, .modules { grid-template-columns: 1fr; } .heading__actions { width: 100%; } }
    @media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto; } }
  `]
})
export class PlataformaComponent {
  private readonly route = inject(ActivatedRoute);
  readonly vistaFlujoV5 = this.route.snapshot.routeConfig?.path === 'plataforma/flujo-v5';
  readonly vistaCoberturaV5 = this.route.snapshot.routeConfig?.path === 'plataforma/cobertura-v5';
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly empresasApi = inject(PlataformaEmpresasService);
  readonly sesion = this.plataforma.sesion;
  // Preferencia visual local con dos estados permitidos; no recibe CSS del cliente ni altera otros tenants.
  readonly contrasteCobertura = signal(false);
  readonly ligerezaCompacta = signal(false);
  // C05-04.M04.03: dos presets locales, no CSS remoto ni permisos procedentes del cliente.
  readonly opcionesMarcaLigereza = [
    { id: 'institucional', nombre: 'Institucional' },
    { id: 'contraste', nombre: 'Contraste alto' }
  ] as const;
  readonly marcaLigereza = signal<'institucional' | 'contraste'>('institucional');

  cambiarMarcaLigereza(id: string): boolean {
    const sesion = this.sesion();
    if (sesion?.ambito !== 'Plataforma' || sesion.esAdministrador !== true) return false;
    const opcion = this.opcionesMarcaLigereza.find(p => p.id === id);
    if (!opcion || this.marcaLigereza() === opcion.id) return false;
    this.marcaLigereza.set(opcion.id);
    return true;
  }
  readonly validando = signal(false);
  readonly error = signal<string | null>(null);
  readonly permisoReadinessDenegado = signal(false);
  readonly directorio = signal<DirectorioPlataforma | null>(null);
  readonly cargandoEmpresas = signal(false);
  readonly errorEmpresas = signal<string | null>(null);
  readonly directorioSinPermiso = signal(false);

  constructor() {
    if (!this.vistaFlujoV5 && !this.vistaCoberturaV5) this.cargarEmpresas();
  }

  cargarEmpresas(pagina = 1): void {
    if (this.cargandoEmpresas()) return;
    this.cargandoEmpresas.set(true);
    this.errorEmpresas.set(null);
    this.directorioSinPermiso.set(false);
    this.directorio.set(null);
    this.empresasApi.listar(pagina).pipe(finalize(() => this.cargandoEmpresas.set(false)))
      .subscribe({
        next: respuesta => this.directorio.set(respuesta),
        error: (err: unknown) => {
          this.directorioSinPermiso.set(err instanceof HttpErrorResponse && (err.status === 401 || err.status === 403));
          this.errorEmpresas.set('No se pudo consultar el directorio global. No se mostrará información de empresas sin autorización verificada.');
        }
      });
  }

  revalidar(): void {
    if (this.validando()) return;
    this.validando.set(true);
    this.error.set(null);
    this.permisoReadinessDenegado.set(false);
    try {
      this.plataforma.obtener().pipe(finalize(() => this.validando.set(false))).subscribe({
        error: (err: unknown) => {
          if (this.vistaFlujoV5 && err instanceof HttpErrorResponse && (err.status === 401 || err.status === 403)) {
            this.permisoReadinessDenegado.set(true);
          } else {
            this.error.set('La sesión global dejó de estar autorizada o no pudo verificarse. No se permite ninguna operación.');
          }
        }
      });
    } catch {
      this.plataforma.limpiar();
      this.validando.set(false);
      this.error.set('La sesión global dejó de estar autorizada o no pudo verificarse. No se permite ninguna operación.');
    }
  }
}
