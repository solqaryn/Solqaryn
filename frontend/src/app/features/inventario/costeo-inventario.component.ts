import { PAGINADOR_ESPANOL_PROVIDER } from '../../shared/paginador-institucional';
import { describirMetodoCosteo, presentarMotivoCosteo } from './costeo-presentacion';
import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CosteoInventarioService } from '../../services/costeo-inventario.service';
import {
  MetodoCosteoInventario,
  MetodoCosteoInventarioOption,
  PoliticaCosteoInventario,
  PoliticaCosteoInventarioQuery
} from './costeo-inventario.model';

@Component({
  viewProviders: [PAGINADOR_ESPANOL_PROVIDER],
  selector: 'app-costeo-inventario',
  standalone: true,
  imports: [ConsultaRegistroDirective,
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  template: `
    <section class="page" aria-labelledby="costeo-title">
      <header class="header">
        <div>
          
          <h1 id="costeo-title" class="page-title">Política de costeo</h1>
          <p class="page-subtitle">Consulta el método utilizado para valorar las existencias, revisa su vigencia y registra cambios en la política de costeo de tu empresa.</p>
        </div>
        <button mat-stroked-button type="button" (click)="recargar()" [disabled]="loading">
          <mat-icon>refresh</mat-icon>Actualizar
        </button>
      </header>
    
      @if (loading && !vigente) {
        <div class="state" aria-live="polite">
          <mat-spinner diameter="36"></mat-spinner><span>Cargando política de costeo…</span>
        </div>
      }
    
      @if (error) {
        <div class="state error" role="alert">
          <mat-icon>error_outline</mat-icon><span>{{ error }}</span>
          <button mat-button type="button" (click)="recargar()">Reintentar</button>
        </div>
      }
    
      @if (vigente) {
        <div class="summary-grid">
          <mat-card>
            <mat-card-header><mat-card-title>Política vigente</mat-card-title></mat-card-header>
            <mat-card-content>
              <div class="current-method"><mat-icon aria-hidden="true">calculate</mat-icon><strong>{{ vigente.metodoNombre }}</strong></div>
              @if (descripcionMetodo(vigente.metodo); as descripcion) {
                <p class="method-description">{{ descripcion }}</p>
              }
              <dl>
                <div><dt>Vigente desde</dt><dd>{{ vigente.vigenteDesdeUtc | date:'dd/MM/yyyy HH:mm' }}</dd></div>
                <div><dt>Motivo</dt><dd>{{ motivoVisible(vigente.motivo, vigente.metodo) }}</dd></div>
                <div><dt>Última actualización</dt><dd>{{ vigente.fechaActualizacion | date:'dd/MM/yyyy HH:mm' }}</dd></div>
              </dl>
            </mat-card-content>
          </mat-card>
          @if (puedeEditar) {
            <mat-card>
              <mat-card-header><mat-card-title>Cambiar política</mat-card-title></mat-card-header>
              <mat-card-content>
                <form class="change-form" (ngSubmit)="cambiarPolitica()">
                  <mat-form-field appearance="outline">
                    <mat-label>Método</mat-label>
                    <mat-select name="metodo" [(ngModel)]="metodoSeleccionado" required>
                      @for (metodo of metodos; track metodo) {
                        <mat-option [value]="metodo.id">{{ metodo.nombre }}</mat-option>
                      }
                    </mat-select>
                  </mat-form-field>
                  @if (metodoSeleccionado !== vigente.metodo) {
                    @if (descripcionMetodo(metodoSeleccionado); as descripcionSeleccionada) {
                      <p class="method-description" aria-live="polite">{{ descripcionSeleccionada }}</p>
                    }
                  }
                  <p class="policy-note">El cambio crea una nueva versión de la política; no modifica las políticas anteriores ni recalcula costos ya registrados.</p>
                  <mat-form-field appearance="outline">
                    <mat-label>Motivo del cambio</mat-label>
                    <textarea matInput name="motivo" [(ngModel)]="motivo" minlength="3" maxlength="500" rows="3" required></textarea>
                    <mat-hint align="end">{{ motivo.length }}/500</mat-hint>
                  </mat-form-field>
                  @if (changeError) {
                    <p class="inline-error" role="alert">{{ changeError }}</p>
                  }
                  @if (changeSuccess) {
                    <p class="inline-success" role="status">{{ changeSuccess }}</p>
                  }
                  <button mat-flat-button color="primary" type="submit" [disabled]="saving || !cambioValido">
                    @if (saving) {
                      <mat-spinner diameter="18"></mat-spinner>
                    }
                    @if (!saving) {
                      <mat-icon>published_with_changes</mat-icon>
                    }
                    {{ saving ? 'Guardando…' : 'Aplicar política' }}
                  </button>
                </form>
              </mat-card-content>
            </mat-card>
          }
        </div>
      }
    
      <mat-card>
        <mat-card-header><mat-card-title>Historial de políticas</mat-card-title></mat-card-header>
        <mat-card-content>
          <form class="filters app-table-filters" (ngSubmit)="aplicarFiltros()" aria-label="Filtros del historial de costeo">
            <mat-form-field appearance="outline">
              <mat-label>Método</mat-label>
              <mat-select name="filtroMetodo" [(ngModel)]="filtroMetodo">
                <mat-option [value]="null">Todos</mat-option>
                @for (metodo of metodos; track metodo) {
                  <mat-option [value]="metodo.id">{{ metodo.nombre }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Vigencia</mat-label>
              <mat-select name="filtroVigente" [(ngModel)]="filtroVigente">
                <mat-option [value]="null">Todas</mat-option>
                <mat-option [value]="true">Vigente</mat-option>
                <mat-option [value]="false">Históricas</mat-option>
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Desde</mat-label>
              <input matInput type="datetime-local" name="desdeUtc" [(ngModel)]="desdeUtc" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Hasta</mat-label>
              <input matInput type="datetime-local" name="hastaUtc" [(ngModel)]="hastaUtc" />
            </mat-form-field>
            <div class="filter-actions">
              <button mat-flat-button color="primary" type="submit">Filtrar</button>
              <button mat-button type="button" (click)="limpiarFiltros()">Limpiar</button>
            </div>
          </form>
    
          @if (filterError) {
            <p class="inline-error" role="alert">{{ filterError }}</p>
          }
          @if (historyLoading) {
            <div class="state compact" aria-live="polite"><mat-spinner diameter="28"></mat-spinner><span>Cargando historial…</span></div>
          }
          @if (!historyLoading && historyError) {
            <div class="state compact error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ historyError }}</span><button mat-button type="button" (click)="cargarHistorial()">Reintentar</button></div>
          }
          @if (!historyLoading && !historyError && historial.length === 0) {
            <div class="state compact empty"><mat-icon>history</mat-icon><span>No hay políticas para los filtros seleccionados.</span></div>
          }
    
          @if (!historyLoading && !historyError && historial.length) {
            <div class="table-wrap app-table-shell" role="region" aria-label="Historial de políticas de costeo" tabindex="0">
              <table class="app-data-table">
                <thead><tr><th>Método</th><th>Desde</th><th>Hasta</th><th>Estado</th><th>Motivo</th><th class="actions">Acciones</th></tr></thead>
                <tbody>
                  @for (politica of historial; track politica) {
                    <tr>
                      <td><strong>{{ politica.metodoNombre }}</strong></td>
                      <td>{{ politica.vigenteDesdeUtc | date:'dd/MM/yyyy HH:mm' }}</td>
                      <td>{{ politica.vigenteHastaUtc ? (politica.vigenteHastaUtc | date:'dd/MM/yyyy HH:mm') : '—' }}</td>
                      <td><span class="badge" [class.active]="politica.estaVigente">{{ politica.estaVigente ? 'Vigente' : 'Histórica' }}</span></td>
                      <td>{{ motivoVisible(politica.motivo, politica.metodo) }}</td><td class="actions"><button class="action-view" mat-icon-button type="button" title="Ver detalle" [attr.aria-label]="'Ver política de costeo ' + politica.id" [appConsultaRegistro]="{ titulo: 'Política de costeo: ' + politica.metodoNombre, campos: { 'Método': politica.metodoNombre, 'Vigente desde': politica.vigenteDesdeUtc, 'Vigente hasta': politica.vigenteHastaUtc, 'Estado': politica.estaVigente ? 'Vigente' : 'Histórica', 'Motivo': motivoVisible(politica.motivo, politica.metodo), 'Fecha de creación': politica.fechaCreacion, 'Fecha de actualización': politica.fechaActualizacion } }"><mat-icon>visibility</mat-icon></button></td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
          @if (totalCount > 0) {
            <mat-paginator aria-label="Paginación del historial de costeo" [length]="totalCount" [pageIndex]="page - 1" [pageSize]="pageSize" [pageSizeOptions]="[10,20,50,100]" (page)="cambiarPagina($event)"></mat-paginator>
          }
        </mat-card-content>
      </mat-card>
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display:block; min-width:0; }
    .page { display:grid; grid-template-columns:minmax(0,1fr); gap:20px; min-width:0; }
    .header { display:flex; justify-content:space-between; gap:16px; align-items:flex-start; }
    .header > div { min-width:0; }
    .header h1 { margin:0; }
    .header p { margin:6px 0 0; max-width:70ch; color:var(--color-text-muted); }
    .summary-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(min(100%,340px),1fr)); gap:20px; min-width:0; }
    mat-card { min-width:0; background:var(--color-surface); color:var(--color-text); border:1px solid var(--color-border); border-radius:var(--radius); box-shadow:var(--shadow-card); }
    mat-card-title { color:var(--color-heading); }
    mat-card-content { min-width:0; }
    .current-method { display:flex; align-items:center; gap:10px; font-size:1.2rem; margin:16px 0 8px; overflow-wrap:anywhere; }
    .current-method mat-icon { flex:none; color:var(--color-primary); }
    .method-description { margin:0 0 16px; color:var(--color-text-muted); line-height:1.5; max-width:65ch; }
    dl { display:grid; gap:10px; margin:0; }
    dl div { display:grid; grid-template-columns:minmax(100px,145px) minmax(0,1fr); gap:12px; }
    dt { font-weight:600; color:var(--color-text-muted); }
    dd { margin:0; min-width:0; overflow-wrap:anywhere; white-space:pre-wrap; }
    .policy-note { margin:0; color:var(--color-text-muted); line-height:1.5; overflow-wrap:anywhere; }
    .change-form { display:grid; grid-template-columns:minmax(0,1fr); gap:10px; margin-top:16px; }
    .change-form button { justify-self:start; }
    .change-form mat-spinner { display:inline-block; margin-right:8px; }
    .filters { margin-top:16px; }
    .state { min-height:120px; display:flex; align-items:center; justify-content:center; flex-wrap:wrap; gap:12px; border:1px dashed var(--color-border); border-radius:var(--radius); padding:16px; text-align:center; overflow-wrap:anywhere; }
    .state.compact { min-height:100px; margin-top:10px; }
    .state.error,.inline-error { color:var(--color-danger); }
    .state.empty { color:var(--color-text-muted); }
    .inline-success { color:var(--color-success); }
    .table-wrap { border:1px solid var(--color-border); border-radius:var(--radius); margin-top:8px; }
    table { min-width:820px; }
    th,td { vertical-align:middle; }
    td:nth-child(5) { min-width:180px; max-width:360px; overflow-wrap:anywhere; white-space:pre-wrap; }
    .badge { display:inline-flex; border-radius:999px; padding:4px 9px; background:var(--color-bg); color:var(--color-text); font-size:.78rem; font-weight:600; }
    .badge.active { background:color-mix(in srgb,var(--color-success) 12%,var(--color-surface)); color:var(--color-text); }
    mat-paginator { background:var(--color-surface); color:var(--color-text); }
    @media(max-width:600px) { .header { flex-direction:column; } dl div { grid-template-columns:minmax(0,1fr); gap:2px; } .change-form button { width:100%; } }
  `]
})
export class CosteoInventarioComponent implements OnInit {
  readonly motivoVisible = presentarMotivoCosteo;
  readonly descripcionMetodo = describirMetodoCosteo;
  vigente: PoliticaCosteoInventario | null = null;
  metodos: MetodoCosteoInventarioOption[] = [];
  historial: PoliticaCosteoInventario[] = [];
  loading = false;
  historyLoading = false;
  saving = false;
  error = '';
  historyError = '';
  filterError = '';
  changeError = '';
  changeSuccess = '';
  metodoSeleccionado: MetodoCosteoInventario | null = null;
  motivo = '';
  filtroMetodo: MetodoCosteoInventario | null = null;
  filtroVigente: boolean | null = null;
  desdeUtc: string | null = null;
  hastaUtc: string | null = null;
  page = 1;
  pageSize = 20;
  totalCount = 0;

  constructor(
    private readonly service: CosteoInventarioService,
    private readonly permisos: PermisosRuntimeService
  ) {}

  ngOnInit(): void { this.recargar(); }

  get puedeEditar(): boolean { return this.permisos.puede('MovimientosInventario', 'Editar'); }
  get cambioValido(): boolean {
    return this.metodoSeleccionado !== null
      && this.metodoSeleccionado !== this.vigente?.metodo
      && this.motivo.trim().length >= 3
      && this.motivo.trim().length <= 500;
  }

  recargar(): void {
    this.loading = true;
    this.error = '';
    forkJoin({ vigente: this.service.getPoliticaVigente(), metodos: this.service.getMetodos() })
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: ({ vigente, metodos }) => {
          if (!vigente.success || !metodos.success) {
            this.error = vigente.message || metodos.message || 'No se pudo cargar la política de costeo.';
            return;
          }
          this.vigente = vigente.data;
          this.metodos = metodos.data;
          this.metodoSeleccionado = vigente.data.metodo;
          this.cargarHistorial();
        },
        error: () => this.error = 'No se pudo cargar la política de costeo.'
      });
  }

  cambiarPolitica(): void {
    if (!this.puedeEditar || this.saving || !this.vigente) return;
    this.changeError = '';
    this.changeSuccess = '';
    const motivo = this.motivo.trim();
    if (!this.cambioValido || this.metodoSeleccionado === null) {
      this.changeError = 'Selecciona un método diferente y registra un motivo de 3 a 500 caracteres.';
      return;
    }
    this.saving = true;
    this.service.cambiarPolitica({ metodo: this.metodoSeleccionado, motivo })
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.changeError = response.message || 'No se pudo actualizar la política de costeo.';
            return;
          }
          this.vigente = response.data;
          this.metodoSeleccionado = response.data.metodo;
          this.motivo = '';
          this.changeSuccess = response.message || 'Política de costeo actualizada correctamente.';
          this.page = 1;
          this.cargarHistorial();
        },
        error: () => this.changeError = 'No se pudo actualizar la política de costeo.'
      });
  }

  aplicarFiltros(): void {
    this.filterError = '';
    const desde = this.parseFecha(this.desdeUtc);
    const hasta = this.parseFecha(this.hastaUtc);
    if ((this.desdeUtc && !desde) || (this.hastaUtc && !hasta)) {
      this.filterError = 'El rango contiene una fecha inválida.';
      return;
    }
    if (desde && hasta && desde.getTime() > hasta.getTime()) {
      this.filterError = 'La fecha “Desde” no puede ser posterior a “Hasta”.';
      return;
    }
    this.page = 1;
    this.cargarHistorial();
  }

  limpiarFiltros(): void {
    this.filtroMetodo = null;
    this.filtroVigente = null;
    this.desdeUtc = null;
    this.hastaUtc = null;
    this.filterError = '';
    this.page = 1;
    this.cargarHistorial();
  }

  cambiarPagina(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargarHistorial();
  }

  cargarHistorial(): void {
    if (this.filterError) return;
    const query: PoliticaCosteoInventarioQuery = {
      page: this.page,
      pageSize: this.pageSize,
      metodo: this.filtroMetodo ?? undefined,
      vigente: this.filtroVigente ?? undefined,
      desdeUtc: this.toIso(this.desdeUtc),
      hastaUtc: this.toIso(this.hastaUtc)
    };
    this.historyLoading = true;
    this.historyError = '';
    this.service.getHistorial(query)
      .pipe(finalize(() => this.historyLoading = false))
      .subscribe({
        next: response => {
          if (!response.success) {
            this.historyError = response.message || 'No se pudo cargar el historial de costeo.';
            return;
          }
          this.historial = response.data.items;
          this.totalCount = response.data.totalCount;
        },
        error: () => this.historyError = 'No se pudo cargar el historial de costeo.'
      });
  }

  private parseFecha(value: string | null): Date | null {
    if (!value) return null;
    const fecha = new Date(value);
    return Number.isNaN(fecha.getTime()) ? null : fecha;
  }

  private toIso(value: string | null): string | undefined {
    return this.parseFecha(value)?.toISOString();
  }
}
