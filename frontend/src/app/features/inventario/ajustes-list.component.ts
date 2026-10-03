import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import {
  AjusteInventario,
  AjusteInventarioFiltro,
  EstadoAjusteInventario
} from '../../core/models/ajuste-inventario.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AjusteInventarioService } from '../../services/ajuste-inventario.service';

@Component({
  selector: 'app-ajustes-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  template: `
    <section class="ajustes-page" aria-labelledby="ajustes-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="ajustes-title">Ajustes de inventario</h1>
          <p class="subtitle">Consulta y controla el ciclo Borrador → Confirmado → Anulado.</p>
        </div>
        <div class="page-actions">
          @if (puedeCrear()) {
            <button mat-flat-button color="primary" type="button" (click)="nuevo()" [disabled]="loading()">
              <mat-icon>add</mat-icon>
              Nuevo ajuste
            </button>
          }
          <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading()">
            <mat-icon>refresh</mat-icon>
            Actualizar
          </button>
        </div>
      </header>
    
      <form class="filters" (ngSubmit)="aplicarFiltros()">
        <mat-form-field appearance="outline">
          <mat-label>Buscar</mat-label>
          <input matInput name="search" [(ngModel)]="search" placeholder="Número o motivo" />
        </mat-form-field>
    
        <mat-form-field appearance="outline">
          <mat-label>Estado</mat-label>
          <mat-select name="estado" [(ngModel)]="estado">
            <mat-option value="">Todos</mat-option>
            <mat-option value="Borrador">Borrador</mat-option>
            <mat-option value="Confirmado">Confirmado</mat-option>
            <mat-option value="Anulado">Anulado</mat-option>
          </mat-select>
        </mat-form-field>
    
        <mat-form-field appearance="outline">
          <mat-label>Desde</mat-label>
          <input matInput type="date" name="desde" [(ngModel)]="desde" />
        </mat-form-field>
    
        <mat-form-field appearance="outline">
          <mat-label>Hasta</mat-label>
          <input matInput type="date" name="hasta" [(ngModel)]="hasta" />
        </mat-form-field>
    
        <div class="filter-actions">
          <button mat-flat-button color="primary" type="submit" [disabled]="loading()">Aplicar</button>
          <button mat-button type="button" (click)="limpiarFiltros()" [disabled]="loading()">Limpiar</button>
        </div>
      </form>
    
      @if (success()) {
        <div class="feedback success" role="status" aria-live="polite">
          <mat-icon>check_circle</mat-icon>
          <span>{{ success() }}</span>
          <button mat-icon-button type="button" aria-label="Cerrar mensaje" (click)="success.set('')">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      }
    
      @if (error()) {
        <div class="feedback error" role="alert">
          <mat-icon>error_outline</mat-icon>
          <span>{{ error() }}</span>
          <button mat-button type="button" (click)="cargar()">Reintentar</button>
        </div>
      }
    
      @if (loading()) {
        <div class="loading" aria-live="polite">
          <mat-spinner diameter="36"></mat-spinner>
          <span>Cargando ajustes…</span>
        </div>
      }
    
      @if (!loading() && !error()) {
        @if (ajustes().length === 0) {
          <div class="empty">
            <mat-icon>warehouse</mat-icon>
            <h2>No hay ajustes para los filtros seleccionados</h2>
            <p>Modifica los filtros o crea un borrador desde el flujo de inventario.</p>
          </div>
        }
        @if (ajustes().length > 0) {
          <div class="table-shell">
            <table>
              <thead>
                <tr>
                  <th>Número</th>
                  <th>Fecha</th>
                  <th>Estado</th>
                  <th>Motivo</th>
                  <th>Detalles</th>
                  <th>Impacto</th>
                  <th class="actions-column">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (ajuste of ajustes(); track trackById($index, ajuste)) {
                  <tr>
                    <td data-label="Número"><strong>{{ ajuste.numeroAjuste }}</strong></td>
                    <td data-label="Fecha">{{ ajuste.fechaAjuste | date:'dd/MM/yyyy HH:mm' }}</td>
                    <td data-label="Estado">
                      <span class="status" [class]="'status ' + ajuste.estado.toLowerCase()">{{ ajuste.estado }}</span>
                    </td>
                    <td data-label="Motivo">{{ ajuste.motivo }}</td>
                    <td data-label="Detalles">{{ ajuste.detalles.length }}</td>
                    <td data-label="Impacto">{{ (ajuste.impactoCostoTotalSnapshot || 0) | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                    <td data-label="Acciones" class="row-actions">
                      <button mat-button type="button" [disabled]="processingId() !== null" (click)="ver(ajuste)">
                        <mat-icon>visibility</mat-icon>
                        Ver
                      </button>
                      @if (puedeEditar() && ajuste.estado === 'Borrador') {
                        <button
                          mat-button
                          color="primary"
                          type="button"
                          [disabled]="processingId() !== null"
                          (click)="editar(ajuste)">
                          <mat-icon>edit</mat-icon>
                          Editar
                        </button>
                      }
                      @if (puedeConfirmar() && ajuste.estado === 'Borrador') {
                        <button
                          mat-stroked-button
                          color="primary"
                          type="button"
                          [disabled]="processingId() === ajuste.id"
                          (click)="confirmar(ajuste)">
                          <mat-icon>check_circle</mat-icon>
                          Confirmar
                        </button>
                      }
                      @if (puedeAnular() && ajuste.estado === 'Confirmado') {
                        <button
                          mat-stroked-button
                          color="warn"
                          type="button"
                          [disabled]="processingId() === ajuste.id"
                          (click)="anular(ajuste)">
                          <mat-icon>undo</mat-icon>
                          Anular
                        </button>
                      }
                      @if (ajuste.estado === 'Anulado') {
                        <span class="muted">Solo lectura</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            <mat-paginator
              [length]="totalCount()"
              [pageIndex]="page - 1"
              [pageSize]="pageSize"
              [pageSizeOptions]="[10, 25, 50]"
              showFirstLastButtons
              (page)="onPageChange($event)">
            </mat-paginator>
          </div>
        }
      }
    </section>
    
    @if (dialogAjuste(); as ajusteDialog) {
      <div class="modal-backdrop" (click)="cerrarModal()">
        <section
          class="modal-card"
          role="dialog"
          aria-modal="true"
          aria-labelledby="inventario-action-dialog-title"
          (click)="$event.stopPropagation()">
          <button
            class="modal-close"
            mat-icon-button
            type="button"
            aria-label="Cerrar"
            [disabled]="processingId() !== null"
            (click)="cerrarModal()">
            <mat-icon>close</mat-icon>
          </button>
          <div class="modal-icon" [class.danger]="dialogAction() === 'anular'">
            <mat-icon>{{ dialogAction() === 'confirmar' ? 'check_circle' : 'undo' }}</mat-icon>
          </div>
          <p class="modal-eyebrow">Inventario empresarial</p>
          <h2 id="inventario-action-dialog-title">
            {{ dialogAction() === 'confirmar' ? 'Confirmar ajuste de inventario' : 'Anular ajuste de inventario' }}
          </h2>
          @if (dialogAction() === 'confirmar') {
            <p class="modal-description">
              Revisa la información antes de continuar. Esta acción aplicará físicamente el inventario y dejará trazabilidad del movimiento.
            </p>
          }
          @if (dialogAction() === 'anular') {
            <p class="modal-description">
              La anulación revertirá el ajuste confirmado y quedará registrada en la trazabilidad. Debes indicar el motivo.
            </p>
          }
          <div class="modal-summary">
            <div><span>Ajuste</span><strong>{{ ajusteDialog.numeroAjuste }}</strong></div>
            <div><span>Motivo</span><strong>{{ ajusteDialog.motivo }}</strong></div>
            <div><span>Detalles</span><strong>{{ ajusteDialog.detalles.length }}</strong></div>
            <div><span>Estado actual</span><strong>{{ ajusteDialog.estado }}</strong></div>
          </div>
          @if (dialogAction() === 'confirmar') {
            <div class="modal-notice">
              <mat-icon>info</mat-icon>
              <div>
                <strong>Acción con efecto real</strong>
                <span>El sistema actualizará las existencias físicas y generará la trazabilidad/Kardex correspondiente.</span>
              </div>
            </div>
          }
          @if (dialogAction() === 'anular') {
            <mat-form-field appearance="outline" class="modal-field">
              <mat-label>Motivo de anulación</mat-label>
              <textarea
                matInput
                rows="4"
                maxlength="500"
                name="motivoAnulacionModal"
                [(ngModel)]="motivoAnulacion"
              placeholder="Describe claramente por qué se anula este ajuste"></textarea>
              <mat-hint align="end">{{ motivoAnulacion.length }}/500</mat-hint>
            </mat-form-field>
          }
          @if (modalError()) {
            <div class="modal-error" role="alert">
              <mat-icon>error_outline</mat-icon>
              <span>{{ modalError() }}</span>
            </div>
          }
          <div class="modal-actions">
            <button mat-button type="button" [disabled]="processingId() !== null" (click)="cerrarModal()">Cancelar</button>
            @if (dialogAction() === 'confirmar') {
              <button
                mat-flat-button
                color="primary"
                type="button"
                [disabled]="processingId() !== null"
                (click)="ejecutarAccionModal()">
                @if (processingId() !== null) {
                  <mat-spinner diameter="18"></mat-spinner>
                }
                @if (processingId() === null) {
                  <mat-icon>check_circle</mat-icon>
                }
                {{ processingId() !== null ? 'Confirmando…' : 'Confirmar ajuste' }}
              </button>
            }
            @if (dialogAction() === 'anular') {
              <button
                mat-flat-button
                color="warn"
                type="button"
                [disabled]="processingId() !== null || !motivoAnulacion.trim()"
                (click)="ejecutarAccionModal()">
                @if (processingId() !== null) {
                  <mat-spinner diameter="18"></mat-spinner>
                }
                @if (processingId() === null) {
                  <mat-icon>undo</mat-icon>
                }
                {{ processingId() !== null ? 'Anulando…' : 'Anular ajuste' }}
              </button>
            }
          </div>
        </section>
      </div>
    }
    `,
  styles: [`
    :host { display: block; }
    .ajustes-page { padding: 24px; max-width: 1500px; margin: 0 auto; }
    .page-header { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 24px; }
    .page-actions { display: flex; gap: 8px; flex-wrap: wrap; justify-content: flex-end; }
    .eyebrow { margin: 0 0 4px; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .08em; opacity: .65; }
    h1 { margin: 0; font-size: clamp(24px, 3vw, 34px); }
    .subtitle { margin: 6px 0 0; opacity: .72; }
    .filters { display: grid; grid-template-columns: minmax(220px, 2fr) repeat(3, minmax(150px, 1fr)) auto; gap: 12px; align-items: start; margin-bottom: 18px; }
    .filter-actions { display: flex; gap: 8px; min-height: 56px; align-items: center; }
    .feedback, .loading, .empty { display: flex; align-items: center; justify-content: center; gap: 10px; padding: 28px; border-radius: 12px; }
    .feedback.error { justify-content: flex-start; border: 1px solid rgba(244,67,54,.32); background: rgba(244,67,54,.06); }
    .feedback.success { justify-content: flex-start; margin-bottom: 16px; border: 1px solid rgba(46,125,50,.28); background: rgba(46,125,50,.07); }
    .feedback.success span { flex: 1; }
    .empty { min-height: 220px; flex-direction: column; text-align: center; border: 1px dashed rgba(127,127,127,.35); }
    .empty h2, .empty p { margin: 0; }
    .empty mat-icon { width: 42px; height: 42px; font-size: 42px; opacity: .5; }
    .table-shell { overflow-x: auto; border: 1px solid rgba(127,127,127,.22); border-radius: 12px; }
    table { width: 100%; border-collapse: collapse; min-width: 1120px; }
    th, td { padding: 14px 16px; text-align: left; border-bottom: 1px solid rgba(127,127,127,.16); vertical-align: middle; }
    th { font-size: 12px; text-transform: uppercase; letter-spacing: .04em; opacity: .72; }
    tbody tr:last-child td { border-bottom: 0; }
    .status { display: inline-flex; padding: 4px 9px; border-radius: 999px; font-size: 12px; font-weight: 700; background: rgba(127,127,127,.14); }
    .status.confirmado { background: rgba(46,125,50,.14); }
    .status.anulado { background: rgba(198,40,40,.14); }
    .status.borrador { background: rgba(245,124,0,.14); }
    .actions-column { width: 310px; }
    .row-actions { display: flex; align-items: center; gap: 6px; white-space: nowrap; }
    .muted { opacity: .55; font-size: 13px; }

    .modal-backdrop {
      position: fixed;
      inset: 0;
      z-index: 1400;
      display: grid;
      place-items: center;
      padding: 24px;
      background: rgba(15,23,42,.58);
      backdrop-filter: blur(5px);
    }
    .modal-card {
      position: relative;
      width: min(620px, 100%);
      max-height: min(760px, calc(100vh - 48px));
      overflow: auto;
      box-sizing: border-box;
      border: 1px solid rgba(127,127,127,.22);
      border-radius: 20px;
      padding: 28px;
      background: var(--surface-card, #fff);
      box-shadow: 0 28px 80px rgba(15,23,42,.34);
    }
    .modal-close { position: absolute; top: 14px; right: 14px; }
    .modal-icon {
      display: grid;
      place-items: center;
      width: 54px;
      height: 54px;
      margin-bottom: 16px;
      border-radius: 16px;
      background: rgba(25,118,210,.12);
    }
    .modal-icon.danger { background: rgba(198,40,40,.1); }
    .modal-icon mat-icon { width: 30px; height: 30px; font-size: 30px; }
    .modal-eyebrow { margin: 0 0 5px; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .08em; opacity: .62; }
    .modal-card h2 { margin: 0; padding-right: 42px; font-size: clamp(22px, 3vw, 28px); }
    .modal-description { margin: 10px 0 20px; line-height: 1.55; opacity: .76; }
    .modal-summary { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; margin-bottom: 18px; }
    .modal-summary div { min-width: 0; padding: 13px 14px; border: 1px solid rgba(127,127,127,.2); border-radius: 12px; background: rgba(127,127,127,.035); }
    .modal-summary span { display: block; margin-bottom: 5px; font-size: 11px; text-transform: uppercase; letter-spacing: .05em; opacity: .6; }
    .modal-summary strong { display: block; overflow: hidden; text-overflow: ellipsis; }
    .modal-notice { display: flex; gap: 12px; align-items: flex-start; margin-bottom: 18px; padding: 14px; border-radius: 12px; background: rgba(25,118,210,.07); }
    .modal-notice div { display: grid; gap: 3px; }
    .modal-notice span { line-height: 1.45; opacity: .76; }
    .modal-field { width: 100%; }
    .modal-error { display: flex; align-items: flex-start; gap: 9px; margin-top: 2px; padding: 12px 14px; border: 1px solid rgba(244,67,54,.28); border-radius: 12px; background: rgba(244,67,54,.06); }
    .modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 22px; }
    .modal-actions mat-spinner { display: inline-block; margin-right: 7px; }

    @media (max-width: 1050px) {
      .filters { grid-template-columns: repeat(2, minmax(0, 1fr)); }
      .filter-actions { grid-column: span 2; }
    }
    @media (max-width: 640px) {
      .ajustes-page { padding: 16px; }
      .page-header { flex-direction: column; }
      .page-actions { width: 100%; justify-content: flex-start; }
      .filters { grid-template-columns: 1fr; }
      .filter-actions { grid-column: auto; }
      .modal-backdrop { padding: 12px; }
      .modal-card { padding: 22px 18px 18px; border-radius: 16px; }
      .modal-summary { grid-template-columns: 1fr; }
      .modal-actions { flex-direction: column-reverse; }
      .modal-actions button { width: 100%; }
    }
  `]
})
export class AjustesListComponent implements OnInit {
  readonly ajustes = signal<AjusteInventario[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly modalError = signal('');
  readonly processingId = signal<number | null>(null);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeConfirmar = signal(false);
  readonly puedeAnular = signal(false);
  readonly dialogAction = signal<'confirmar' | 'anular' | null>(null);
  readonly dialogAjuste = signal<AjusteInventario | null>(null);

  search = '';
  estado: '' | EstadoAjusteInventario = '';
  desde = '';
  hasta = '';
  page = 1;
  pageSize = 10;
  motivoAnulacion = '';

  constructor(
    private readonly ajusteService: AjusteInventarioService,
    private readonly permisosRuntime: PermisosRuntimeService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Inventario', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Inventario', 'Editar'));
    this.puedeConfirmar.set(this.permisosRuntime.puede('Inventario', 'Confirmar'));
    this.puedeAnular.set(this.permisosRuntime.puede('Inventario', 'Anular'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set('');

    const filtro: AjusteInventarioFiltro = {
      page: this.page,
      pageSize: this.pageSize,
      search: this.search.trim() || undefined,
      estado: this.estado || undefined,
      desde: this.desde || undefined,
      hasta: this.hasta || undefined,
      sortBy: 'fechaAjuste',
      sortDirection: 'desc'
    };

    this.ajusteService.getPaged(filtro)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this.ajustes.set([]);
            this.totalCount.set(0);
            this.error.set(this.extraerRespuestaFallida(response, 'No fue posible cargar los ajustes.'));
            return;
          }

          this.ajustes.set(response.data.items);
          this.totalCount.set(response.data.totalCount);
          this.page = response.data.page;
          this.pageSize = response.data.pageSize;
        },
        error: (err) => {
          this.ajustes.set([]);
          this.totalCount.set(0);
          this.error.set(this.extraerError(err, 'No fue posible cargar los ajustes.'));
        }
      });
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.search = '';
    this.estado = '';
    this.desde = '';
    this.hasta = '';
    this.page = 1;
    this.pageSize = 10;
    this.cargar();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }

  nuevo(): void {
    if (!this.puedeCrear()) return;
    void this.router.navigate(['/inventario/ajustes/nuevo']);
  }

  ver(ajuste: AjusteInventario): void {
    void this.router.navigate(['/inventario/ajustes', ajuste.id]);
  }

  editar(ajuste: AjusteInventario): void {
    if (!this.puedeEditar() || ajuste.estado !== 'Borrador') return;
    void this.router.navigate(['/inventario/ajustes', ajuste.id, 'editar']);
  }

  confirmar(ajuste: AjusteInventario): void {
    if (!this.puedeConfirmar() || ajuste.estado !== 'Borrador' || this.processingId() !== null) return;
    this.abrirModal('confirmar', ajuste);
  }

  anular(ajuste: AjusteInventario): void {
    if (!this.puedeAnular() || ajuste.estado !== 'Confirmado' || this.processingId() !== null) return;
    this.abrirModal('anular', ajuste);
  }

  cerrarModal(): void {
    if (this.processingId() !== null) return;
    this.resetModal();
  }

  ejecutarAccionModal(): void {
    const accion = this.dialogAction();
    const ajuste = this.dialogAjuste();
    if (!accion || !ajuste || this.processingId() !== null) return;

    this.modalError.set('');
    if (accion === 'confirmar') {
      this.ejecutarConfirmacion(ajuste);
      return;
    }

    const motivo = this.motivoAnulacion.trim();
    if (!motivo) {
      this.modalError.set('Indica un motivo claro antes de anular el ajuste.');
      return;
    }
    this.ejecutarAnulacion(ajuste, motivo);
  }

  trackById(_: number, ajuste: AjusteInventario): number {
    return ajuste.id;
  }

  private abrirModal(accion: 'confirmar' | 'anular', ajuste: AjusteInventario): void {
    this.success.set('');
    this.modalError.set('');
    this.motivoAnulacion = '';
    this.dialogAction.set(accion);
    this.dialogAjuste.set(ajuste);
  }

  private ejecutarConfirmacion(ajuste: AjusteInventario): void {
    this.processingId.set(ajuste.id);
    this.ajusteService.confirmar(ajuste.id)
      .pipe(finalize(() => this.processingId.set(null)))
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this.modalError.set(this.extraerRespuestaFallida(response, 'No fue posible confirmar el ajuste.'));
            return;
          }
          this.resetModal();
          this.success.set(`El ajuste ${ajuste.numeroAjuste} fue confirmado correctamente y el inventario quedó actualizado.`);
          this.cargar();
        },
        error: (err) => this.modalError.set(this.extraerError(err, 'No fue posible confirmar el ajuste.'))
      });
  }

  private ejecutarAnulacion(ajuste: AjusteInventario, motivo: string): void {
    this.processingId.set(ajuste.id);
    this.ajusteService.anular(ajuste.id, motivo)
      .pipe(finalize(() => this.processingId.set(null)))
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this.modalError.set(this.extraerRespuestaFallida(response, 'No fue posible anular el ajuste.'));
            return;
          }
          this.resetModal();
          this.success.set(`El ajuste ${ajuste.numeroAjuste} fue anulado correctamente y la reversión quedó registrada.`);
          this.cargar();
        },
        error: (err) => this.modalError.set(this.extraerError(err, 'No fue posible anular el ajuste.'))
      });
  }

  private resetModal(): void {
    this.dialogAction.set(null);
    this.dialogAjuste.set(null);
    this.modalError.set('');
    this.motivoAnulacion = '';
  }

  private extraerRespuestaFallida(response: { message?: string; errors?: string[] }, fallback: string): string {
    if (typeof response.message === 'string' && response.message.trim()) return response.message;
    if (Array.isArray(response.errors) && response.errors.length) return response.errors.join(' ');
    return fallback;
  }

  private extraerError(error: any, fallback: string): string {
    const api = error?.error;
    if (typeof api?.message === 'string' && api.message.trim()) return api.message;
    if (Array.isArray(api?.errors) && api.errors.length) return api.errors.join(' ');
    return fallback;
  }
}
