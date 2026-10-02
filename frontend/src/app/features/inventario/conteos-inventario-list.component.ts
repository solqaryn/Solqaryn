import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
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
  selector: 'app-conteos-inventario-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="conteos-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="conteos-title">Conteos físicos</h1>
          <p>Gestiona conteos generales, cíclicos, por ubicación, categoría y ciegos con trazabilidad de diferencias.</p>
        </div>
        <button *ngIf="puedeCrear" mat-flat-button color="primary" type="button" (click)="nuevo()">
          <mat-icon>fact_check</mat-icon>Nuevo conteo
        </button>
      </header>

      <form class="filters" (ngSubmit)="aplicarFiltros()">
        <mat-form-field appearance="outline"><mat-label>Buscar</mat-label><input matInput name="search" [(ngModel)]="search" placeholder="Número, almacén..." /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Estado</mat-label><mat-select name="estado" [(ngModel)]="estado"><mat-option [value]="null">Todos</mat-option><mat-option *ngFor="let item of estados" [value]="item.value">{{ item.label }}</mat-option></mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Tipo</mat-label><mat-select name="tipo" [(ngModel)]="tipo"><mat-option [value]="null">Todos</mat-option><mat-option *ngFor="let item of tipos" [value]="item.value">{{ item.label }}</mat-option></mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Almacén</mat-label><mat-select name="almacen" [(ngModel)]="almacenId"><mat-option [value]="null">Todos</mat-option><mat-option *ngFor="let almacen of almacenes" [value]="almacen.id">{{ almacen.codigo }} · {{ almacen.nombre }}</mat-option></mat-select></mat-form-field>
        <div class="filter-actions"><button mat-flat-button color="primary" type="submit">Filtrar</button><button mat-button type="button" (click)="limpiar()">Limpiar</button></div>
      </form>

      <div *ngIf="catalogoError" class="catalog-warning" role="status"><mat-icon>info</mat-icon><span>{{ catalogoError }}</span><button mat-button type="button" (click)="cargarAlmacenes()">Reintentar catálogo</button></div>
      <div *ngIf="loading" class="state" aria-live="polite"><mat-spinner diameter="36"></mat-spinner><span>Cargando conteos…</span></div>
      <div *ngIf="!loading && error" class="state error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error }}</span><button mat-button type="button" (click)="cargar()">Reintentar</button></div>
      <div *ngIf="!loading && !error && items.length === 0" class="state empty"><mat-icon>warehouse</mat-icon><strong>No hay conteos para los filtros seleccionados.</strong></div>

      <div *ngIf="!loading && !error && items.length" class="table-wrap">
        <table>
          <thead><tr><th>Número</th><th>Tipo</th><th>Estado</th><th>Almacén / ubicación</th><th>Captura</th><th>Diferencias</th><th>Acciones</th></tr></thead>
          <tbody>
            <tr *ngFor="let item of items">
              <td><strong>{{ item.numero }}</strong><small *ngIf="item.esCiego">Conteo ciego</small></td>
              <td>{{ item.tipoNombre }}</td>
              <td><span class="badge" [attr.data-estado]="item.estadoNombre">{{ item.estadoNombre }}</span></td>
              <td>{{ item.almacenNombre || ('#' + item.almacenId) }}<small *ngIf="item.ubicacionNombre">{{ item.ubicacionNombre }}</small></td>
              <td>{{ item.cantidadCapturadas }} / {{ item.cantidadLineas }}</td>
              <td><strong [class.diff]="item.cantidadConDiferencia > 0">{{ item.cantidadConDiferencia }}</strong><small>Neto: {{ item.diferenciaNeta }}</small></td>
              <td class="actions"><button mat-icon-button type="button" aria-label="Ver conteo" (click)="ver(item.id)"><mat-icon>visibility</mat-icon></button><button *ngIf="puedeEditar && item.estado === EstadoConteoInventario.Borrador" mat-icon-button type="button" aria-label="Editar conteo" (click)="editar(item.id)"><mat-icon>edit</mat-icon></button></td>
            </tr>
          </tbody>
        </table>
      </div>

      <mat-paginator *ngIf="totalCount > 0" [length]="totalCount" [pageIndex]="page - 1" [pageSize]="pageSize" [pageSizeOptions]="[10,20,50,100]" (page)="cambiarPagina($event)"></mat-paginator>
    </section>
  `,
  styles: [`
    .page{padding:24px;display:grid;gap:20px}.header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.header h1{margin:0;font-size:1.75rem}.header p{margin:6px 0 0;color:var(--text-secondary,#667085)}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:.72rem;font-weight:700;color:var(--primary,#3f51b5)!important}.header button mat-icon{margin-right:6px}.filters{display:grid;grid-template-columns:2fr 1fr 1fr 1fr auto;gap:12px;align-items:start}.filter-actions{display:flex;gap:6px;padding-top:4px}.catalog-warning{display:flex;align-items:center;gap:8px;padding:10px 12px;border-radius:10px;background:#fffaeb;color:#7a2e0e}.state{min-height:180px;display:flex;align-items:center;justify-content:center;gap:12px;border:1px dashed #d0d5dd;border-radius:12px;padding:24px}.state.error{color:#b42318}.state.empty{flex-direction:column;color:#667085}.table-wrap{overflow:auto;border:1px solid #e4e7ec;border-radius:12px}table{width:100%;border-collapse:collapse;min-width:900px}th,td{padding:14px 16px;text-align:left;border-bottom:1px solid #eaecf0;vertical-align:middle}th{font-size:.78rem;text-transform:uppercase;letter-spacing:.04em;color:#667085;background:#f9fafb}td small{display:block;margin-top:3px;color:#667085}.badge{display:inline-flex;border-radius:999px;padding:4px 9px;background:#f2f4f7;font-size:.78rem;font-weight:600}.diff{color:#b54708}.actions{white-space:nowrap}@media(max-width:900px){.page{padding:16px}.header{flex-direction:column}.filters{grid-template-columns:1fr 1fr}.filter-actions{grid-column:1/-1}}@media(max-width:560px){.filters{grid-template-columns:1fr}}
  `]
})
export class ConteosInventarioListComponent implements OnInit {
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
    private readonly almacenesService: AlmacenService,
    private readonly router: Router,
    private readonly permisos: PermisosRuntimeService
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
  nuevo(): void { void this.router.navigate(['/inventario/conteos/nuevo']); }
  ver(id: number): void { void this.router.navigate(['/inventario/conteos', id]); }
  editar(id: number): void { void this.router.navigate(['/inventario/conteos', id, 'editar']); }
}
