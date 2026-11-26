import { PAGINADOR_ESPANOL_PROVIDER } from '../../shared/paginador-institucional';
import { crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';

import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { Almacen } from '../../core/models/almacen.model';
import {
  ConteoInventario,
  ConteoInventarioFiltro,
  EstadoConteoInventario,
  TipoConteoInventario
} from '../../core/models/conteo-inventario.model';
import { AlmacenService } from '../../services/almacen.service';
import { ConteoInventarioService } from '../../services/conteo-inventario.service';

@Component({
  viewProviders: [PAGINADOR_ESPANOL_PROVIDER],
  selector: 'app-conteos-inventario-list',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="conteos-title">
      <header class="header">
        <div>
          
          <h1 id="conteos-title">Conteos físicos</h1>
          <p>Registra conteos físicos y revisa las diferencias de inventario.</p>
        </div>
        @if (puedeCrear) {
          <button mat-flat-button color="primary" type="button" (click)="nuevo()">
            <mat-icon>fact_check</mat-icon>Nuevo conteo
          </button>
        }
      </header>
      <div class="card app-list-surface">
    
      <form class="filters app-table-filters" (ngSubmit)="aplicarFiltros()">
        <mat-form-field appearance="outline"><mat-label>Buscar</mat-label><input matInput name="search" [(ngModel)]="search" placeholder="Número, almacén..." /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Estado</mat-label><mat-select name="estado" [(ngModel)]="estado"><mat-option [value]="null">Todos</mat-option>@for (item of estados; track item) {
        <mat-option [value]="item.value">{{ item.label }}</mat-option>
      }</mat-select></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Tipo</mat-label><mat-select name="tipo" [(ngModel)]="tipo"><mat-option [value]="null">Todos</mat-option>@for (item of tipos; track item) {
      <mat-option [value]="item.value">{{ item.label }}</mat-option>
    }</mat-select></mat-form-field>
    <mat-form-field appearance="outline"><mat-label>Almacén</mat-label><mat-select name="almacen" [(ngModel)]="almacenId"><mat-option [value]="null">Todos</mat-option>@for (almacen of almacenes; track almacen) {
    <mat-option [value]="almacen.id">{{ almacen.codigo }} · {{ almacen.nombre }}</mat-option>
    }</mat-select></mat-form-field>
    <div class="filter-actions"><button mat-flat-button color="primary" type="submit">Filtrar</button><button mat-button type="button" (click)="limpiar()">Limpiar</button></div>
    </form>
    
    @if (catalogoError) {
      <div class="catalog-warning" role="status"><mat-icon>info</mat-icon><span>{{ catalogoError }}</span><button mat-button type="button" (click)="cargarAlmacenes()">Reintentar catálogo</button></div>
    }
    @if (loading) {
      <div class="state" aria-live="polite"><mat-spinner diameter="36"></mat-spinner><span>Cargando conteos…</span></div>
    }
    @if (!loading && error) {
      <div class="state error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error }}</span><button mat-button type="button" (click)="cargar()">Reintentar</button></div>
    }
    @if (!loading && !error && items.length === 0) {
      <div class="state empty app-list-empty"><mat-icon>warehouse</mat-icon><strong>No se encontraron conteos</strong><p>Cambia los filtros para ampliar la búsqueda.</p></div>
    }
    
    @if (!loading && !error && items.length) {
      <div class="table-wrap app-table-shell" role="region" tabindex="0" aria-label="conteos">
        <table class="app-data-table">
          <thead><tr><th>Número</th><th>Tipo</th><th>Estado</th><th>Almacén / ubicación</th><th>Captura</th><th>Diferencias</th><th class="actions-heading">Acciones</th></tr></thead>
          <tbody>
            @for (item of items; track item) {
              <tr>
                <td><strong>{{ item.numero }}</strong>@if (item.esCiego) {
                <small>Conteo ciego</small>
              }</td>
              <td>{{ item.tipoNombre }}</td>
              <td><span class="badge" [attr.data-estado]="item.estadoNombre">{{ item.estadoNombre }}</span></td>
              <td>{{ item.almacenNombre || ('#' + item.almacenId) }}@if (item.ubicacionNombre) {
                <small>{{ item.ubicacionNombre }}</small>
              }</td>
              <td>{{ item.cantidadCapturadas }} / {{ item.cantidadLineas }}</td>
              <td><strong [class.diff]="item.cantidadConDiferencia > 0">{{ item.cantidadConDiferencia }}</strong><small>Neto: {{ item.diferenciaNeta }}</small></td>
              <td class="actions"><button class="action-view" mat-icon-button type="button" aria-label="Ver conteo" (click)="ver(item.id)"><mat-icon>visibility</mat-icon></button>@if (puedeEditar && item.estado === EstadoConteoInventario.Borrador) {
              <button class="action-edit" mat-icon-button type="button" aria-label="Editar conteo" (click)="editar(item.id)"><mat-icon>edit</mat-icon></button>
            }</td>
          </tr>
        }
      </tbody>
    </table>
    </div>
    }
    
    @if (totalCount > 0) {
      <mat-paginator [length]="totalCount" [pageIndex]="page - 1" [pageSize]="pageSize" [pageSizeOptions]="[10,20,50,100]" (page)="cambiarPagina($event)"></mat-paginator>
    }
    </div>
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page{padding:24px;display:grid;grid-template-columns:minmax(0,1fr);gap:20px;min-width:0}.header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.header h1{margin:0;font-size:1.75rem}.header p{margin:6px 0 0;color:var(--text-secondary,#667085)}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:.72rem;font-weight:700;color:var(--primary,#3f51b5)!important}.header button mat-icon{margin-right:6px}.catalog-warning{display:flex;align-items:center;gap:8px;padding:10px 12px;border-radius:10px;background:#fffaeb;color:#7a2e0e}.state{min-height:180px;display:flex;align-items:center;justify-content:center;gap:12px;border-radius:12px;padding:24px}.state.error{color:#b42318}.table-wrap{overflow:auto;border:1px solid #e4e7ec;border-radius:12px}table{width:100%;border-collapse:collapse;min-width:900px}th,td{padding:14px 16px;text-align:left;border-bottom:1px solid #eaecf0;vertical-align:middle}th{font-size:.78rem;text-transform:uppercase;letter-spacing:.04em;color:#667085;background:#f9fafb}td small{display:block;margin-top:3px;color:#667085}.badge{display:inline-flex;border-radius:999px;padding:4px 9px;background:#f2f4f7;font-size:.78rem;font-weight:600}.diff{color:#b54708}.actions{white-space:nowrap}@media(max-width:900px){.page{padding:16px}.header{flex-direction:column}}@media(max-width:560px){}
  `]
})
export class ConteosInventarioListComponent implements OnInit {
  readonly acciones = crearAccionesInstitucionales('MovimientosInventario');
  readonly EstadoConteoInventario = EstadoConteoInventario;
  items: ConteoInventario[] = [];
  almacenes: Almacen[] = [];
  loading = false;
  error = '';
  catalogoError = '';
  totalCount = 0;
  page = 1;
  pageSize = 20;
  search = '';
  estado: EstadoConteoInventario | null = null;
  tipo: TipoConteoInventario | null = null;
  almacenId: number | null = null;

  readonly estados = [
    { value: EstadoConteoInventario.Borrador, label: 'Borrador' },
    { value: EstadoConteoInventario.EnProceso, label: 'En proceso' },
    { value: EstadoConteoInventario.Cerrado, label: 'Cerrado' },
    { value: EstadoConteoInventario.Aprobado, label: 'Aprobado' },
    { value: EstadoConteoInventario.Cancelado, label: 'Cancelado' }
  ];
  readonly tipos = [
    { value: TipoConteoInventario.General, label: 'General' },
    { value: TipoConteoInventario.Ciclico, label: 'Cíclico' },
    { value: TipoConteoInventario.PorUbicacion, label: 'Por ubicación' },
    { value: TipoConteoInventario.PorCategoria, label: 'Por categoría' },
    { value: TipoConteoInventario.Ciego, label: 'Ciego' }
  ];

  constructor(
    private readonly service: ConteoInventarioService,
    private readonly almacenesService: AlmacenService, private readonly permisos: PermisosRuntimeService
  ) {}

  ngOnInit(): void { this.cargarAlmacenes(); this.cargar(); }
  get puedeCrear(): boolean { return this.permisos.puede('MovimientosInventario', 'Crear'); }
  get puedeEditar(): boolean { return this.permisos.puede('MovimientosInventario', 'Editar'); }

  cargarAlmacenes(): void {
    this.catalogoError = '';
    this.almacenesService.getActivos().subscribe({
      next: response => { if (response.success) this.almacenes = response.data; else this.catalogoError = response.message || 'No se pudo cargar el catálogo de almacenes.'; },
      error: () => this.catalogoError = 'No se pudo cargar el catálogo de almacenes.'
    });
  }

  cargar(): void {
    this.loading = true; this.error = '';
    const filtro: ConteoInventarioFiltro = { page: this.page, pageSize: this.pageSize, search: this.search.trim() || undefined, estado: this.estado ?? undefined, tipo: this.tipo ?? undefined, almacenId: this.almacenId || undefined };
    this.service.getPaged(filtro).pipe(finalize(() => this.loading = false)).subscribe({
      next: response => { if (!response.success) { this.error = response.message || 'No se pudieron cargar los conteos.'; return; } this.items = response.data.items; this.totalCount = response.data.totalCount; },
      error: () => this.error = 'No se pudieron cargar los conteos.'
    });
  }
  aplicarFiltros(): void { this.page = 1; this.cargar(); }
  limpiar(): void { this.search = ''; this.estado = null; this.tipo = null; this.almacenId = null; this.page = 1; this.cargar(); }
  cambiarPagina(event: PageEvent): void { this.page = event.pageIndex + 1; this.pageSize = event.pageSize; this.cargar(); }
  async nuevo(): Promise<void> { if (await this.acciones.abrir(() => import('./conteo-inventario-form.component').then(m => m.ConteoInventarioFormComponent), 'xl', 'Crear')) this.cargar(); }
  async ver(id: number): Promise<void> { if (await this.acciones.abrir(() => import('./conteo-inventario-detail.component').then(m => m.ConteoInventarioDetailComponent), 'xl', 'Ver', id)) this.cargar(); }
  async editar(id: number): Promise<void> { if (this.items.find(item => item.id === id)?.estado !== EstadoConteoInventario.Borrador) return; if (await this.acciones.abrir(() => import('./conteo-inventario-form.component').then(m => m.ConteoInventarioFormComponent), 'xl', 'Editar', id)) this.cargar(); }
}
