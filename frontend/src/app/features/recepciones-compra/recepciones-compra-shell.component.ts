import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
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
import { EstadoRecepcionCompra, EstadoRecepcionCompraNombre, RecepcionCompra } from '../../core/models/recepcion-compra.model';
import { RecepcionCompraService } from '../../services/recepcion-compra.service';

@Component({
  selector: 'app-recepciones-compra-shell',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page-shell" aria-labelledby="recepciones-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Compras empresariales</p>
          <h1 id="recepciones-title">Recepción de mercancía</h1>
          <p>Consulta recepciones totales o parciales contra órdenes de compra. El stock físico sólo cambia al confirmar una recepción.</p>
        </div>
        @if (puedeCrear()) {
          <button mat-flat-button type="button" (click)="nuevaRecepcion()"><mat-icon>add_box</mat-icon> Nueva recepción</button>
        }
      </header>

      @if (!puedeVer()) {
        <div class="state-panel error" role="alert"><mat-icon>lock</mat-icon><span>No tienes permiso para consultar recepciones de compra.</span></div>
      } @else {
        <form class="filters" (ngSubmit)="aplicarFiltros()" aria-label="Filtros de recepciones de compra">
          <mat-form-field appearance="outline">
            <mat-label>ID orden</mat-label>
            <input matInput type="number" min="1" [(ngModel)]="ordenCompraId" name="ordenCompraId">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Estado</mat-label>
            <mat-select [(ngModel)]="estado" name="estado">
              <mat-option value="">Todos</mat-option>
              @for (item of estados; track item) { <mat-option [value]="item">{{ item }}</mat-option> }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Desde</mat-label>
            <input matInput type="date" [(ngModel)]="desde" name="desde">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Hasta</mat-label>
            <input matInput type="date" [(ngModel)]="hasta" name="hasta">
          </mat-form-field>
          <div class="filter-actions">
            <button mat-flat-button type="submit"><mat-icon>search</mat-icon> Filtrar</button>
            <button mat-button type="button" (click)="limpiarFiltros()">Limpiar</button>
          </div>
        </form>

        @if (loading()) {
          <div class="state-panel" role="status"><mat-spinner diameter="36"></mat-spinner><span>Cargando recepciones…</span></div>
        } @else if (error()) {
          <div class="state-panel error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error() }}</span><button mat-stroked-button type="button" (click)="cargar()">Reintentar</button></div>
        } @else if (recepciones().length === 0) {
          <div class="state-panel" role="status"><mat-icon>move_to_inbox</mat-icon><span>No hay recepciones que coincidan con los filtros.</span></div>
        } @else {
          <div class="table-wrap">
            <table>
              <thead><tr><th>Recepción</th><th>Orden</th><th>Estado</th><th>Fecha</th><th class="numeric">Recibida</th><th class="numeric">Aceptada</th><th class="numeric">Dañada</th><th class="numeric">Faltante</th><th class="numeric">Sobrante</th><th>Acciones</th></tr></thead>
              <tbody>
                @for (item of recepciones(); track item.id) {
                  <tr>
                    <td><strong>{{ item.numeroRecepcion }}</strong></td>
                    <td>{{ item.numeroOrdenCompra || ('#' + item.ordenCompraId) }}</td>
                    <td><span class="status" [attr.data-status]="item.estado">{{ etiquetaEstado(item.estado) }}</span></td>
                    <td>{{ item.fechaRecepcionUtc ? (item.fechaRecepcionUtc | date:'medium') : 'Borrador' }}</td>
                    <td class="numeric">{{ item.cantidadRecibidaTotal }}</td>
                    <td class="numeric">{{ item.cantidadAceptadaTotal }}</td>
                    <td class="numeric">{{ item.cantidadDanadaTotal }}</td>
                    <td class="numeric">{{ item.cantidadFaltanteTotal }}</td>
                    <td class="numeric">{{ item.cantidadSobranteTotal }}</td>
                    <td><button mat-stroked-button type="button" (click)="verDetalle(item.id)"><mat-icon>visibility</mat-icon> Ver</button></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <mat-paginator [length]="totalCount()" [pageIndex]="page - 1" [pageSize]="pageSize" [pageSizeOptions]="[10,20,50,100]" (page)="cambiarPagina($event)" aria-label="Paginación de recepciones de compra"></mat-paginator>
        }
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page-shell{display:grid;gap:1.25rem;max-width:1500px;margin:0 auto}.page-header{display:flex;justify-content:space-between;align-items:flex-start;gap:1rem}.eyebrow{margin:0 0 .25rem;text-transform:uppercase;letter-spacing:.08em;font-size:.75rem;font-weight:700;opacity:.7}h1{margin:.1rem 0}.filters{display:grid;grid-template-columns:repeat(4,minmax(150px,1fr)) auto;gap:.75rem;align-items:start}.filter-actions{display:flex;gap:.5rem;align-items:center;padding-top:.25rem}.state-panel{min-height:120px;display:flex;align-items:center;justify-content:center;gap:.75rem;border:1px solid rgba(127,127,127,.18);border-radius:14px}.error{color:var(--mat-sys-error,#b3261e)}.table-wrap{overflow:auto;border:1px solid rgba(127,127,127,.18);border-radius:12px}table{width:100%;border-collapse:collapse;min-width:1100px}th,td{padding:.72rem .75rem;text-align:left;border-bottom:1px solid rgba(127,127,127,.15)}th{font-size:.78rem;text-transform:uppercase;letter-spacing:.04em}.numeric{text-align:right}.status{font-weight:700}@media(max-width:1050px){.filters{grid-template-columns:repeat(2,minmax(160px,1fr))}.filter-actions{grid-column:1/-1}}@media(max-width:680px){.page-header{flex-direction:column}.filters{grid-template-columns:1fr}}
  `]
})
export class RecepcionesCompraShellComponent implements OnInit {
  readonly estados: EstadoRecepcionCompraNombre[] = ['Borrador', 'Recibida', 'Anulada'];
  readonly recepciones = signal<RecepcionCompra[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly puedeVer = signal(false);
  readonly puedeCrear = signal(false);

  page = 1;
  pageSize = 20;
  ordenCompraId: number | null = null;
  estado = '';
  desde = '';
  hasta = '';

  constructor(private readonly service: RecepcionCompraService, private readonly router: Router, public readonly permisosRuntime: PermisosRuntimeService) {}

  ngOnInit(): void {
    this.puedeVer.set(this.permisosRuntime.puede('Compras', 'Ver'));
    this.puedeCrear.set(this.permisosRuntime.puede('Compras', 'Crear'));
    if (this.puedeVer()) this.cargar();
  }

  cargar(): void {
    if (!this.puedeVer()) return;
    this.loading.set(true); this.error.set('');
    this.service.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      ordenCompraId: this.ordenCompraId,
      estado: (this.estado || null) as EstadoRecepcionCompraNombre | null,
      desdeUtc: this.desde ? new Date(`${this.desde}T00:00:00`).toISOString() : null,
      hastaUtc: this.finDiaUtc(this.hasta)
    }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => { this.recepciones.set(response.data?.items ?? []); this.totalCount.set(response.data?.totalCount ?? 0); },
      error: () => this.error.set('No fue posible cargar las recepciones de compra.')
    });
  }

  aplicarFiltros(): void { if (!this.puedeVer()) return; this.page = 1; this.cargar(); }
  limpiarFiltros(): void { if (!this.puedeVer()) return; this.ordenCompraId = null; this.estado = ''; this.desde = ''; this.hasta = ''; this.page = 1; this.cargar(); }
  cambiarPagina(event: PageEvent): void { if (!this.puedeVer()) return; this.page = event.pageIndex + 1; this.pageSize = event.pageSize; this.cargar(); }
  nuevaRecepcion(): void { if (this.puedeCrear()) void this.router.navigate(['/recepciones-compra/nueva']); }
  verDetalle(id: number): void { if (this.puedeVer()) void this.router.navigate(['/recepciones-compra', id]); }

  etiquetaEstado(estado: EstadoRecepcionCompra): string {
    return ({ '1': 'Borrador', '2': 'Recibida', '3': 'Anulada', Borrador: 'Borrador', Recibida: 'Recibida', Anulada: 'Anulada' } as Record<string,string>)[String(estado)] ?? String(estado);
  }

  private finDiaUtc(fecha: string): string | null {
    if (!fecha) return null;
    const siguienteMedianocheLocal = new Date(`${fecha}T00:00:00`);
    siguienteMedianocheLocal.setDate(siguienteMedianocheLocal.getDate() + 1);
    const ultimoSegundo = new Date(siguienteMedianocheLocal.getTime() - 1000).toISOString();
    return ultimoSegundo.replace(/\.\d{3}Z$/, '.9999999Z');
  }
}
