import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
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
import { EstadoReservaInventario, ReservaInventario, ReservaInventarioFiltro } from '../../core/models/reserva-inventario.model';
import { AlmacenService } from '../../services/almacen.service';
import { ReservaInventarioService } from '../../services/reserva-inventario.service';

@Component({
  selector: 'app-reservas-inventario-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="reservas-title">
      <header class="header">
        <div><p class="eyebrow">Inventario empresarial</p><h1 id="reservas-title">Reservas de inventario</h1><p>Controla stock reservado y disponible sin permitir sobreventa.</p></div>
        @if (puedeCrear) {
          <button mat-flat-button color="primary" type="button" (click)="nuevo()"><mat-icon>bookmark_add</mat-icon>Nueva reserva</button>
        }
      </header>
    
      <form class="filters" (ngSubmit)="aplicarFiltros()">
        <mat-form-field appearance="outline"><mat-label>Buscar</mat-label><input matInput name="busqueda" [(ngModel)]="busqueda" placeholder="Número de reserva" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Estado</mat-label><mat-select name="estado" [(ngModel)]="estado"><mat-option [value]="null">Todos</mat-option>@for (item of estados; track item) {
        <mat-option [value]="item">{{ item }}</mat-option>
      }</mat-select></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Venta</mat-label><input matInput type="number" min="1" name="ventaId" [(ngModel)]="ventaId" /></mat-form-field>
      <mat-form-field appearance="outline"><mat-label>Almacén</mat-label><mat-select name="almacenId" [(ngModel)]="almacenId"><mat-option [value]="null">Todos</mat-option>@for (almacen of almacenes; track almacen) {
      <mat-option [value]="almacen.id">{{ almacen.codigo }} · {{ almacen.nombre }}</mat-option>
    }</mat-select></mat-form-field>
    <mat-form-field appearance="outline"><mat-label>Expira desde</mat-label><input matInput type="datetime-local" name="expiraDesde" [(ngModel)]="expiraDesde" /></mat-form-field>
    <mat-form-field appearance="outline"><mat-label>Expira hasta</mat-label><input matInput type="datetime-local" name="expiraHasta" [(ngModel)]="expiraHasta" /></mat-form-field>
    <div class="filter-actions"><button mat-flat-button color="primary" type="submit">Filtrar</button><button mat-button type="button" (click)="limpiar()">Limpiar</button></div>
    </form>
    
    @if (catalogoError) {
      <div class="catalog-warning" role="status"><mat-icon>info</mat-icon><span>{{ catalogoError }}</span><button mat-button type="button" (click)="cargarAlmacenes()">Reintentar catálogo</button></div>
    }
    @if (loading) {
      <div class="state" aria-live="polite"><mat-spinner diameter="36"></mat-spinner><span>Cargando reservas…</span></div>
    }
    @if (!loading && error) {
      <div class="state error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error }}</span>@if (!errorFiltros) {
      <button mat-button type="button" (click)="cargar()">Reintentar</button>
    }</div>
    }
    @if (!loading && !error && items.length === 0) {
      <div class="state empty"><mat-icon>bookmark_border</mat-icon><strong>No hay reservas para los filtros seleccionados.</strong></div>
    }
    
    @if (!loading && !error && items.length) {
      <div class="table-wrap"><table><thead><tr><th>Número</th><th>Estado</th><th>Venta</th><th>Expira</th><th>Detalle</th><th>Acciones</th></tr></thead><tbody>@for (item of items; track item) {
      <tr><td><strong>{{ item.numero }}</strong><small>{{ item.fechaCreacion | date:'short' }}</small></td><td><span class="badge">{{ item.estado }}</span></td><td>{{ item.ventaId ? ('#' + item.ventaId) : '—' }}</td><td>{{ item.fechaExpiracion ? (item.fechaExpiracion | date:'short') : 'Sin expiración' }}</td><td>{{ item.detalles.length }} línea(s)<small>{{ totalReservado(item) }} unidades reservadas</small></td><td class="actions"><button mat-icon-button type="button" aria-label="Ver reserva" (click)="ver(item.id)"><mat-icon>visibility</mat-icon></button>@if (puedeEditar && item.estado === 'Borrador') {
      <button mat-icon-button type="button" aria-label="Editar reserva" (click)="editar(item.id)"><mat-icon>edit</mat-icon></button>
    }</td></tr>
    }</tbody></table></div>
    }
    @if (totalCount > 0) {
      <mat-paginator [length]="totalCount" [pageIndex]="page - 1" [pageSize]="pageSize" [pageSizeOptions]="[10,20,50,100]" (page)="cambiarPagina($event)"></mat-paginator>
    }
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`.page{padding:24px;display:grid;gap:20px}.header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.header h1{margin:0;font-size:1.75rem}.header p{margin:6px 0 0;color:var(--text-secondary,#667085)}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:.72rem;font-weight:700;color:var(--primary,#3f51b5)!important}.header button mat-icon{margin-right:6px}.filters{display:grid;grid-template-columns:2fr repeat(5,minmax(140px,1fr)) auto;gap:12px;align-items:start}.filter-actions{display:flex;gap:6px;padding-top:4px}.catalog-warning{display:flex;align-items:center;gap:8px;padding:10px 12px;border-radius:10px;background:#fffaeb;color:#7a2e0e}.state{min-height:180px;display:flex;align-items:center;justify-content:center;gap:12px;border:1px dashed #d0d5dd;border-radius:12px;padding:24px}.state.error{color:#b42318}.state.empty{flex-direction:column;color:#667085}.table-wrap{overflow:auto;border:1px solid #e4e7ec;border-radius:12px}table{width:100%;border-collapse:collapse;min-width:820px}th,td{padding:14px 16px;text-align:left;border-bottom:1px solid #eaecf0;vertical-align:middle}th{font-size:.78rem;text-transform:uppercase;letter-spacing:.04em;color:#667085;background:#f9fafb}td small{display:block;margin-top:3px;color:#667085}.badge{display:inline-flex;border-radius:999px;padding:4px 9px;background:#f2f4f7;font-size:.78rem;font-weight:600}.actions{white-space:nowrap}@media(max-width:1200px){.filters{grid-template-columns:repeat(3,1fr)}.filter-actions{grid-column:1/-1}}@media(max-width:900px){.page{padding:16px}.header{flex-direction:column}.filters{grid-template-columns:1fr 1fr}.filter-actions{grid-column:1/-1}}@media(max-width:560px){.filters{grid-template-columns:1fr}}`]
})
export class ReservasInventarioListComponent implements OnInit {
  items: ReservaInventario[] = [];
  almacenes: Almacen[] = [];
  loading = false;
  error = '';
  errorFiltros = false;
  catalogoError = '';
  totalCount = 0;
  page = 1;
  pageSize = 20;
  busqueda = '';
  estado: EstadoReservaInventario | null = null;
  ventaId: number | null = null;
  almacenId: number | null = null;
  expiraDesde: string | null = null;
  expiraHasta: string | null = null;
  readonly estados: EstadoReservaInventario[] = ['Borrador', 'Activa', 'Consumida', 'Liberada', 'Expirada', 'Cancelada'];

  constructor(private readonly service: ReservaInventarioService, private readonly almacenesService: AlmacenService, private readonly router: Router, private readonly permisos: PermisosRuntimeService) {}

  ngOnInit(): void { this.cargarAlmacenes(); this.cargar(); }
  get puedeCrear(): boolean { return this.permisos.puede('MovimientosInventario', 'Crear'); }
  get puedeEditar(): boolean { return this.permisos.puede('MovimientosInventario', 'Editar'); }

  cargarAlmacenes(): void { this.catalogoError = ''; this.almacenesService.getActivos().subscribe({ next: response => { if (response.success) this.almacenes = response.data; else this.catalogoError = response.message || 'No se pudo cargar el catálogo de almacenes.'; }, error: () => this.catalogoError = 'No se pudo cargar el catálogo de almacenes.' }); }
  cargar(): void {
    this.loading = true;
    this.error = '';
    this.errorFiltros = false;
    const filtro: ReservaInventarioFiltro = {
      page: this.page,
      pageSize: this.pageSize,
      busqueda: this.busqueda.trim() || undefined,
      estado: this.estado ?? undefined,
      ventaId: this.ventaId || undefined,
      almacenId: this.almacenId || undefined,
      expiraDesde: this.toIso(this.expiraDesde),
      expiraHasta: this.toIso(this.expiraHasta)
    };
    this.service.getPaged(filtro).pipe(finalize(() => this.loading = false)).subscribe({ next: response => { if (!response.success) { this.error = response.message || 'No se pudieron cargar las reservas.'; return; } this.items = response.data.items; this.totalCount = response.data.totalCount; }, error: () => this.error = 'No se pudieron cargar las reservas.' });
  }
  aplicarFiltros(): void {
    const desde = this.parseFecha(this.expiraDesde);
    const hasta = this.parseFecha(this.expiraHasta);
    if ((this.expiraDesde && !desde) || (this.expiraHasta && !hasta)) {
      this.error = 'El rango de expiración contiene una fecha inválida.';
      this.errorFiltros = true;
      return;
    }
    if (desde && hasta && desde.getTime() > hasta.getTime()) {
      this.error = 'La fecha “Expira desde” no puede ser posterior a “Expira hasta”.';
      this.errorFiltros = true;
      return;
    }
    this.page = 1;
    this.cargar();
  }
  limpiar(): void { this.busqueda = ''; this.estado = null; this.ventaId = null; this.almacenId = null; this.expiraDesde = null; this.expiraHasta = null; this.page = 1; this.error = ''; this.errorFiltros = false; this.cargar(); }
  cambiarPagina(event: PageEvent): void { this.page = event.pageIndex + 1; this.pageSize = event.pageSize; this.cargar(); }
  totalReservado(item: ReservaInventario): number { return item.detalles.reduce((total, detalle) => total + detalle.cantidadReservada, 0); }
  nuevo(): void { void this.router.navigate(['/inventario/reservas/nueva']); }
  ver(id: number): void { void this.router.navigate(['/inventario/reservas', id]); }
  editar(id: number): void { void this.router.navigate(['/inventario/reservas', id, 'editar']); }

  private parseFecha(value: string | null): Date | null {
    if (!value) return null;
    const fecha = new Date(value);
    return Number.isNaN(fecha.getTime()) ? null : fecha;
  }

  private toIso(value: string | null): string | undefined {
    return this.parseFecha(value)?.toISOString();
  }
}
