
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
import {
  EstadoTransferenciaInventario,
  TransferenciaInventario,
  TransferenciaInventarioFiltro
} from '../../core/models/transferencia-inventario.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TransferenciaInventarioService } from '../../services/transferencia-inventario.service';

@Component({
  selector: 'app-transferencias-list',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="transferencias-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="transferencias-title">Transferencias de inventario</h1>
          <p>Controla solicitud, aprobación, tránsito, recepción y cancelación entre almacenes.</p>
        </div>
        @if (puedeCrear) {
          <button mat-flat-button color="primary" type="button" (click)="nueva()">
            <mat-icon>swap_horiz</mat-icon>Nueva transferencia
          </button>
        }
      </header>
    
      <form class="filters" (ngSubmit)="aplicarFiltros()">
        <mat-form-field appearance="outline"><mat-label>Número</mat-label><input matInput name="numero" [(ngModel)]="numero" /></mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Estado</mat-label>
          <mat-select name="estado" [(ngModel)]="estado">
            <mat-option [value]="null">Todos</mat-option>
            @for (option of estados; track option) {
              <mat-option [value]="option.value">{{ option.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Almacén origen</mat-label><input matInput type="number" min="1" name="origen" [(ngModel)]="almacenOrigenId" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Almacén destino</mat-label><input matInput type="number" min="1" name="destino" [(ngModel)]="almacenDestinoId" /></mat-form-field>
        <div class="filter-actions">
          <button mat-flat-button color="primary" type="submit" [disabled]="loading">Buscar</button>
          <button mat-stroked-button type="button" (click)="limpiar()" [disabled]="loading">Limpiar</button>
        </div>
      </form>
    
      @if (loading) {
        <div class="state"><mat-spinner diameter="36"></mat-spinner><span>Cargando transferencias…</span></div>
      }
      @if (!loading && error) {
        <div class="state error">{{ error }}</div>
      }
      @if (!loading && !error && items.length === 0) {
        <div class="state">No hay transferencias que coincidan con los filtros.</div>
      }
    
      @if (!loading && items.length > 0) {
        <div class="table-wrap">
          <table>
            <thead><tr><th>Número</th><th>Origen</th><th>Destino</th><th>Estado</th><th>Detalles</th><th>Acciones</th></tr></thead>
            <tbody>
              @for (item of items; track item) {
                <tr>
                  <td><strong>{{ item.numero }}</strong></td>
                  <td>{{ item.almacenOrigenNombre || ('#' + item.almacenOrigenId) }}</td>
                  <td>{{ item.almacenDestinoNombre || ('#' + item.almacenDestinoId) }}</td>
                  <td><span class="status" [attr.data-state]="item.estado">{{ item.estado }}</span></td>
                  <td>{{ item.detalles.length }}</td>
                  <td class="actions">
                    <button mat-button type="button" (click)="ver(item.id)">Ver</button>
                    @if (puedeEditar && item.estado === 'Borrador') {
                      <button mat-button type="button" (click)="editar(item.id)">Editar</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    
      <mat-paginator [length]="totalCount" [pageIndex]="page - 1" [pageSize]="pageSize" [pageSizeOptions]="[10, 20, 50]" (page)="cambiarPagina($event)"></mat-paginator>
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page{padding:24px;display:grid;gap:20px}.header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:12px;font-weight:700;margin:0}.header h1{margin:4px 0}.header p{margin:0;color:var(--text-secondary,#667085)}.filters{display:grid;grid-template-columns:repeat(4,minmax(150px,1fr)) auto;gap:12px;align-items:start}.filter-actions{display:flex;gap:8px;padding-top:4px}.state{min-height:100px;display:flex;gap:12px;align-items:center;justify-content:center}.error{color:#b42318}.table-wrap{overflow:auto;border:1px solid rgba(0,0,0,.12);border-radius:12px}table{width:100%;border-collapse:collapse}th,td{padding:14px;text-align:left;border-bottom:1px solid rgba(0,0,0,.08);white-space:nowrap}.actions{display:flex;gap:4px}.status{display:inline-flex;padding:4px 10px;border-radius:999px;background:rgba(0,0,0,.06);font-weight:600}@media(max-width:1000px){.filters{grid-template-columns:1fr 1fr}.header{flex-direction:column}}@media(max-width:640px){.page{padding:16px}.filters{grid-template-columns:1fr}}
  `]
})
export class TransferenciasListComponent implements OnInit {
  items: TransferenciaInventario[] = [];
  loading = false;
  error = '';
  page = 1;
  pageSize = 20;
  totalCount = 0;
  numero = '';
  estado: EstadoTransferenciaInventario | null = null;
  almacenOrigenId: number | null = null;
  almacenDestinoId: number | null = null;

  readonly estados = [
    { value: EstadoTransferenciaInventario.Borrador, label: 'Borrador' },
    { value: EstadoTransferenciaInventario.Solicitada, label: 'Solicitada' },
    { value: EstadoTransferenciaInventario.Aprobada, label: 'Aprobada' },
    { value: EstadoTransferenciaInventario.EnTransito, label: 'En tránsito' },
    { value: EstadoTransferenciaInventario.Recibida, label: 'Recibida' },
    { value: EstadoTransferenciaInventario.Cancelada, label: 'Cancelada' }
  ];

  constructor(
    private readonly service: TransferenciaInventarioService,
    private readonly router: Router,
    private readonly permisos: PermisosRuntimeService
  ) {}

  ngOnInit(): void { this.cargar(); }

  get puedeCrear(): boolean { return this.permisos.puede('MovimientosInventario', 'Crear'); }
  get puedeEditar(): boolean { return this.permisos.puede('MovimientosInventario', 'Editar'); }

  cargar(): void {
    this.loading = true;
    this.error = '';
    const filtro: TransferenciaInventarioFiltro = {
      page: this.page,
      pageSize: this.pageSize,
      sortBy: 'FechaCreacion',
      sortDirection: 'desc',
      numero: this.numero.trim() || undefined,
      estado: this.estado ?? undefined,
      almacenOrigenId: this.almacenOrigenId || undefined,
      almacenDestinoId: this.almacenDestinoId || undefined
    };
    this.service.getPaged(filtro).pipe(finalize(() => this.loading = false)).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudieron cargar las transferencias.'; return; }
        this.items = response.data.items;
        this.totalCount = response.data.totalCount;
      },
      error: () => this.error = 'No se pudieron cargar las transferencias.'
    });
  }

  aplicarFiltros(): void { this.page = 1; this.cargar(); }
  limpiar(): void { this.numero = ''; this.estado = null; this.almacenOrigenId = null; this.almacenDestinoId = null; this.page = 1; this.cargar(); }
  cambiarPagina(event: PageEvent): void { this.page = event.pageIndex + 1; this.pageSize = event.pageSize; this.cargar(); }
  nueva(): void { void this.router.navigate(['/inventario/transferencias/nueva']); }
  ver(id: number): void { void this.router.navigate(['/inventario/transferencias', id]); }
  editar(id: number): void { void this.router.navigate(['/inventario/transferencias', id, 'editar']); }
}
