import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { ApiResponse } from '../../core/models/api-response.model';

type ModoEnvio = 'directo' | 'plantilla';

interface EmailEmpresarialResultado {
  mensajeId: string;
  empresaId: number;
  destinatario: string;
  asunto: string | null;
  plantillaCodigo: string | null;
  plantillaVersion: number | null;
  estado: number | string;
  intentos: number;
  correlationId: string | null;
  providerMessageId: string | null;
  creadoEnUtc: string;
  disponibleDesdeUtc: string;
  entregadoEnUtc: string | null;
  rebotadoEnUtc: string | null;
}

interface PaginaEmailEmpresarial {
  items: EmailEmpresarialResultado[];
  pagina: number;
  tamano: number;
  total: number;
}

interface RegistrarEmailEmpresarialRequest {
  destinatario: string;
  asunto?: string;
  cuerpoHtml?: string;
  cuerpoTexto?: string;
  plantillaCodigo?: string;
  plantillaVersion?: number;
  variablesJson?: string;
  correlationId?: string;
}

interface ProblemDetailsLike {
  detail?: string;
  title?: string;
}

interface EstadoOpcion {
  valor: number | null;
  etiqueta: string;
}

@Component({
  selector: 'app-email-empresarial-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="card email-card" aria-labelledby="email-empresarial-title">
      <div class="header-row">
        <div>
          <h2 id="email-empresarial-title">Correo empresarial</h2>
          <p class="subtitle">
            Registra envíos idempotentes y consulta la cola, reintentos, tracking y rebotes del tenant activo sin exponer secretos del proveedor.
          </p>
        </div>
        <button type="button" class="secondary-button" [disabled]="loading() || !puedeVer() || !empresaId()" (click)="cargar()">
          {{ loading() ? 'Actualizando…' : 'Actualizar cola' }}
        </button>
      </div>

      @if (!empresaId()) {
        <div class="notice warning" role="status">Selecciona una empresa activa y verificada para administrar el correo empresarial.</div>
      } @else if (!puedeVer()) {
        <div class="notice warning" role="status">Necesitas “Configuración: Ver” para consultar el correo empresarial.</div>
      } @else {
        <div class="tenant-summary">
          <span>Tenant verificado</span>
          <strong>#{{ empresaId() }}</strong>
          <small>La API vuelve a comprobar la membresía server-side.</small>
        </div>

        <div class="layout-grid">
          <section class="panel" aria-labelledby="email-envio-title">
            <div class="panel-heading">
              <h3 id="email-envio-title">Nuevo envío</h3>
              <span class="idempotency" title="Se conserva durante reintentos fallidos">Idempotency-Key: {{ claveIdempotencia() }}</span>
            </div>

            @if (!puedeCrear()) {
              <div class="notice warning" role="status">Necesitas “Configuración: Crear” para registrar nuevos envíos.</div>
            }

            <div class="mode-switch" role="group" aria-label="Modo de envío">
              <button type="button" [class.active]="modo() === 'directo'" [disabled]="sending() || !puedeCrear()" (click)="cambiarModo('directo')">Contenido directo</button>
              <button type="button" [class.active]="modo() === 'plantilla'" [disabled]="sending() || !puedeCrear()" (click)="cambiarModo('plantilla')">Plantilla versionada</button>
            </div>

            <label>
              <span>Destinatario</span>
              <input type="email" [(ngModel)]="destinatario" [disabled]="sending() || !puedeCrear()" placeholder="cliente@empresa.com" autocomplete="off">
            </label>

            @if (modo() === 'directo') {
              <label>
                <span>Asunto</span>
                <input type="text" [(ngModel)]="asunto" [disabled]="sending() || !puedeCrear()" maxlength="998">
              </label>
              <label>
                <span>Contenido HTML</span>
                <textarea [(ngModel)]="cuerpoHtml" [disabled]="sending() || !puedeCrear()" rows="4" maxlength="65535"></textarea>
              </label>
              <label>
                <span>Contenido de texto <small>(opcional)</small></span>
                <textarea [(ngModel)]="cuerpoTexto" [disabled]="sending() || !puedeCrear()" rows="4" maxlength="65535"></textarea>
              </label>
            } @else {
              <div class="two-columns">
                <label>
                  <span>Código de plantilla</span>
                  <input type="text" [(ngModel)]="plantillaCodigo" [disabled]="sending() || !puedeCrear()" maxlength="120">
                </label>
                <label>
                  <span>Versión</span>
                  <input type="number" [(ngModel)]="plantillaVersion" [disabled]="sending() || !puedeCrear()" min="1" step="1">
                </label>
              </div>
              <label>
                <span>Variables JSON</span>
                <textarea [(ngModel)]="variablesJson" [disabled]="sending() || !puedeCrear()" rows="4" maxlength="65535" placeholder='{"cliente":"Ana"}'></textarea>
              </label>
            }

            <label>
              <span>Correlation ID <small>(opcional)</small></span>
              <input type="text" [(ngModel)]="correlationId" [disabled]="sending() || !puedeCrear()" maxlength="160">
            </label>

            <div class="actions">
              <button type="button" class="primary-button" [disabled]="sending() || !puedeCrear() || !envioValido()" (click)="registrar()">
                {{ sending() ? 'Registrando…' : 'Registrar en cola' }}
              </button>
              <button type="button" class="text-button" [disabled]="sending()" (click)="limpiarBorrador()">Limpiar</button>
            </div>
            <p class="hint">Un error de red conserva la misma clave de idempotencia para que “Reintentar” no duplique el correo.</p>
          </section>

          <section class="panel" aria-labelledby="email-cola-title">
            <div class="panel-heading">
              <h3 id="email-cola-title">Cola y tracking</h3>
              <span>{{ total() }} registro(s)</span>
            </div>

            <div class="filters">
              <label>
                <span>Estado</span>
                <select [(ngModel)]="estadoFiltro" [disabled]="loading()" (change)="aplicarFiltros()">
                  @for (opcion of estados; track opcion.etiqueta) {
                    <option [ngValue]="opcion.valor">{{ opcion.etiqueta }}</option>
                  }
                </select>
              </label>
              <label>
                <span>Correlation ID</span>
                <input type="text" [(ngModel)]="correlationFiltro" [disabled]="loading()" maxlength="160" (keyup.enter)="aplicarFiltros()">
              </label>
              <button type="button" class="secondary-button" [disabled]="loading()" (click)="aplicarFiltros()">Aplicar filtros</button>
            </div>

            @if (loading()) {
              <div class="notice" role="status" aria-live="polite">Consultando cola de correo empresarial…</div>
            } @else if (items().length === 0) {
              <div class="empty-state">No hay correos que coincidan con los filtros actuales.</div>
            } @else {
              <div class="table-wrap">
                <table>
                  <thead><tr><th>Destinatario</th><th>Asunto / plantilla</th><th>Estado</th><th>Intentos</th><th>Correlation</th><th>Creado</th><th>Entrega / rebote</th></tr></thead>
                  <tbody>
                    @for (item of items(); track item.mensajeId) {
                      <tr>
                        <td><strong>{{ item.destinatario }}</strong><small>{{ item.mensajeId }}</small></td>
                        <td>{{ item.plantillaCodigo ? (item.plantillaCodigo + ' v' + item.plantillaVersion) : (item.asunto || 'Sin asunto') }}</td>
                        <td><span class="status-chip" [attr.data-state]="etiquetaEstado(item.estado)">{{ etiquetaEstado(item.estado) }}</span></td>
                        <td>{{ item.intentos }}</td>
                        <td>{{ item.correlationId || '—' }}</td>
                        <td>{{ item.creadoEnUtc | date:'short' }}</td>
                        <td>
                          @if (item.entregadoEnUtc) { Entregado {{ item.entregadoEnUtc | date:'short' }} }
                          @else if (item.rebotadoEnUtc) { Rebotado {{ item.rebotadoEnUtc | date:'short' }} }
                          @else { Disponible {{ item.disponibleDesdeUtc | date:'short' }} }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
              <div class="pagination" aria-label="Paginación de correo empresarial">
                <button type="button" class="secondary-button" [disabled]="loading() || pagina() <= 1" (click)="irPagina(pagina() - 1)">Anterior</button>
                <span>Página {{ pagina() }} · {{ items().length }} de {{ total() }}</span>
                <button type="button" class="secondary-button" [disabled]="loading() || pagina() * tamano >= total()" (click)="irPagina(pagina() + 1)">Siguiente</button>
              </div>
            }
          </section>
        </div>

        @if (error()) { <div class="notice error" role="alert">{{ error() }}</div> }
        @if (success()) { <div class="notice success" role="status">{{ success() }}</div> }
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .email-card { margin-top: 1rem; padding: 1.25rem; }
    .header-row, .panel-heading, .actions, .pagination { display: flex; justify-content: space-between; align-items: center; gap: .75rem; flex-wrap: wrap; }
    h2, h3 { margin: 0; }
    .subtitle { margin: .35rem 0 0; color: var(--text-secondary, #5f6368); max-width: 80ch; }
    .tenant-summary { display: flex; gap: .75rem; align-items: baseline; flex-wrap: wrap; margin-top: 1rem; padding: .75rem 1rem; border: 1px solid rgba(0,0,0,.12); border-radius: .65rem; }
    .tenant-summary small { color: var(--text-secondary, #5f6368); }
    .layout-grid { display: grid; grid-template-columns: minmax(300px, .8fr) minmax(0, 1.6fr); gap: 1rem; margin-top: 1rem; }
    .panel { border: 1px solid rgba(0,0,0,.12); border-radius: .75rem; padding: 1rem; min-width: 0; }
    .idempotency { font: 600 .72rem/1.3 monospace; overflow-wrap: anywhere; color: var(--text-secondary, #5f6368); }
    label { display: grid; gap: .35rem; margin-top: .8rem; font-size: .88rem; font-weight: 600; }
    label small, .hint { color: var(--text-secondary, #5f6368); font-weight: 400; }
    input, textarea, select { width: 100%; box-sizing: border-box; border: 1px solid rgba(0,0,0,.22); border-radius: .5rem; padding: .65rem .7rem; font: inherit; background: var(--surface-color, #fff); color: inherit; }
    textarea { resize: vertical; }
    .two-columns, .filters { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .75rem; align-items: end; }
    .filters { grid-template-columns: minmax(150px, .7fr) minmax(180px, 1fr) auto; margin-top: .75rem; }
    .mode-switch { display: flex; gap: .4rem; margin-top: .85rem; }
    .mode-switch button, .secondary-button, .text-button, .primary-button { border-radius: .5rem; padding: .6rem .8rem; font: inherit; font-weight: 600; cursor: pointer; }
    .mode-switch button, .secondary-button { border: 1px solid rgba(0,0,0,.22); background: transparent; color: inherit; }
    .mode-switch button.active { border-color: var(--primary-color, #2563eb); color: var(--primary-color, #2563eb); }
    .primary-button { border: 0; background: var(--primary-color, #2563eb); color: #fff; }
    .text-button { border: 0; background: transparent; color: var(--primary-color, #2563eb); }
    button:disabled { opacity: .55; cursor: not-allowed; }
    .notice, .empty-state { margin-top: 1rem; border-radius: .65rem; padding: .85rem 1rem; background: rgba(0,0,0,.04); }
    .notice.warning { border-left: 4px solid #b26a00; }
    .notice.error { border-left: 4px solid #b3261e; }
    .notice.success { border-left: 4px solid #137333; }
    .table-wrap { margin-top: .8rem; overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; font-size: .84rem; }
    th, td { text-align: left; vertical-align: top; border-bottom: 1px solid rgba(0,0,0,.1); padding: .65rem .5rem; }
    td small { display: block; margin-top: .2rem; color: var(--text-secondary, #5f6368); max-width: 16rem; overflow-wrap: anywhere; }
    .status-chip { display: inline-block; border: 1px solid currentColor; border-radius: 999px; padding: .2rem .45rem; font-size: .72rem; font-weight: 700; white-space: nowrap; }
    .pagination { margin-top: .8rem; }
    @media (max-width: 960px) { .layout-grid { grid-template-columns: 1fr; } }
    @media (max-width: 640px) { .header-row, .panel-heading { align-items: stretch; } .two-columns, .filters { grid-template-columns: 1fr; } .mode-switch { display: grid; } }
  `]
})
export class EmailEmpresarialCardComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly tenantContext = inject(TenantContextService);
  private readonly apiBase = `${environment.apiUrl}/email-empresarial/tenants`;

  readonly loading = signal(false);
  readonly sending = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly items = signal<EmailEmpresarialResultado[]>([]);
  readonly total = signal(0);
  readonly pagina = signal(1);
  readonly tamano = 25;
  readonly modo = signal<ModoEnvio>('directo');
  readonly claveIdempotencia = signal(this.nuevaClaveIdempotencia());
  readonly empresaId = computed(() => this.tenantContext.empresaIdVerificada());
  readonly puedeVer = computed(() => this.permisos.puede('Configuracion', 'Ver'));
  readonly puedeCrear = computed(() => this.permisos.puede('Configuracion', 'Crear'));

  readonly estados: EstadoOpcion[] = [
    { valor: null, etiqueta: 'Todos' },
    { valor: 0, etiqueta: 'Pendiente' },
    { valor: 1, etiqueta: 'Procesando' },
    { valor: 2, etiqueta: 'Reintento pendiente' },
    { valor: 3, etiqueta: 'Aceptado por proveedor' },
    { valor: 4, etiqueta: 'Entregado' },
    { valor: 5, etiqueta: 'Rebotado' },
    { valor: 6, etiqueta: 'Fallido final' }
  ];

  destinatario = '';
  asunto = '';
  cuerpoTexto = '';
  cuerpoHtml = '';
  plantillaCodigo = '';
  plantillaVersion = 1;
  variablesJson = '{}';
  correlationId = '';
  estadoFiltro: number | null = null;
  correlationFiltro = '';

  ngOnInit(): void {
    if (this.empresaId() && this.puedeVer()) this.cargar();
  }

  envioValido(): boolean {
    if (!this.destinatario.trim() || !this.destinatario.includes('@')) return false;
    if (this.modo() === 'plantilla') {
      return Boolean(this.plantillaCodigo.trim() && this.plantillaVersion > 0 && this.variablesJson.trim());
    }
    return Boolean(this.asunto.trim() && this.cuerpoHtml.trim());
  }

  cambiarModo(modo: ModoEnvio): void {
    this.modo.set(modo);
    this.error.set(null);
  }

  registrar(): void {
    const empresaId = this.empresaId();
    if (!empresaId || !this.puedeCrear() || !this.envioValido()) {
      this.error.set('No existe un tenant verificado, permiso suficiente o un borrador válido.');
      return;
    }

    const request = this.crearRequest();
    const headers = new HttpHeaders({ 'Idempotency-Key': this.claveIdempotencia() });
    this.sending.set(true);
    this.error.set(null);
    this.success.set(null);

    this.http.post<ApiResponse<EmailEmpresarialResultado>>(
      `${this.apiBase}/${empresaId}`,
      request,
      { headers }
    ).pipe(finalize(() => this.sending.set(false))).subscribe({
      next: (response) => {
        this.success.set(`Correo ${response.data.mensajeId} registrado de forma durable.`);
        this.limpiarBorrador();
        this.cargar();
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(this.detalleError(err, 'No se pudo registrar el correo. Puedes reintentar con la misma clave sin duplicarlo.'));
      }
    });
  }

  cargar(): void {
    const empresaId = this.empresaId();
    if (!empresaId || !this.puedeVer()) {
      this.items.set([]);
      this.total.set(0);
      return;
    }

    let params = new HttpParams()
      .set('pagina', String(this.pagina()))
      .set('tamano', String(this.tamano));
    if (this.estadoFiltro !== null) params = params.set('estado', String(this.estadoFiltro));
    if (this.correlationFiltro.trim()) params = params.set('correlationId', this.correlationFiltro.trim());

    this.loading.set(true);
    this.error.set(null);
    this.http.get<ApiResponse<PaginaEmailEmpresarial>>(
      `${this.apiBase}/${empresaId}`,
      { params }
    ).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (response) => {
        this.items.set(response.data.items);
        this.total.set(response.data.total);
        this.pagina.set(response.data.pagina);
      },
      error: (err: HttpErrorResponse) => {
        this.items.set([]);
        this.total.set(0);
        this.error.set(this.detalleError(err, 'No se pudo consultar la cola de correo empresarial.'));
      }
    });
  }

  aplicarFiltros(): void {
    this.pagina.set(1);
    this.cargar();
  }

  irPagina(pagina: number): void {
    if (pagina < 1) return;
    this.pagina.set(pagina);
    this.cargar();
  }

  limpiarBorrador(): void {
    this.destinatario = '';
    this.asunto = '';
    this.cuerpoTexto = '';
    this.cuerpoHtml = '';
    this.plantillaCodigo = '';
    this.plantillaVersion = 1;
    this.variablesJson = '{}';
    this.correlationId = '';
    this.modo.set('directo');
    this.claveIdempotencia.set(this.nuevaClaveIdempotencia());
  }

  etiquetaEstado(estado: number | string): string {
    const numerico = typeof estado === 'number' ? estado : Number(estado);
    return this.estados.find(opcion => opcion.valor === numerico)?.etiqueta ?? String(estado);
  }

  private crearRequest(): RegistrarEmailEmpresarialRequest {
    const correlationId = this.correlationId.trim() || undefined;
    if (this.modo() === 'plantilla') {
      return {
        destinatario: this.destinatario.trim(),
        plantillaCodigo: this.plantillaCodigo.trim(),
        plantillaVersion: this.plantillaVersion,
        variablesJson: this.variablesJson.trim(),
        correlationId
      };
    }

    return {
      destinatario: this.destinatario.trim(),
      asunto: this.asunto.trim(),
      cuerpoHtml: this.cuerpoHtml.trim(),
      cuerpoTexto: this.cuerpoTexto.trim() || undefined,
      correlationId
    };
  }

  private nuevaClaveIdempotencia(): string {
    return globalThis.crypto?.randomUUID?.() ?? `solqaryn-email-${Date.now()}-${Math.random().toString(36).slice(2)}`;
  }

  private detalleError(error: HttpErrorResponse, fallback: string): string {
    const problem = error.error as ProblemDetailsLike | null;
    return problem?.detail || problem?.title || fallback;
  }
}
