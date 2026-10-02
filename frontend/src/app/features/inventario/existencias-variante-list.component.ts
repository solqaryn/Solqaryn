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
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ExistenciaVariante, ExistenciaVarianteFiltro } from '../../core/models/existencia-variante.model';
import { ExistenciaVarianteService } from '../../services/existencia-variante.service';

@Component({
  selector: 'app-existencias-variante-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="existencias-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="existencias-title">Existencias por variante</h1>
          <p class="subtitle">Stock físico autoritativo por variante, almacén y ubicación.</p>
        </div>
        <div class="header-actions">
          <button *ngIf="puedeCrear()" mat-flat-button color="primary" type="button" (click)="nueva()" [disabled]="loading()">
            <mat-icon>add</mat-icon> Nueva existencia
          </button>
          <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading()">
            <mat-icon>refresh</mat-icon> Actualizar
          </button>
        </div>
      </header>

      <form class="filters" (ngSubmit)="aplicarFiltros()" aria-label="Filtros de existencias">
        <mat-form-field appearance="outline"><mat-label>Variante ID</mat-label><input matInput type="number" min="1" name="productoVarianteId" [(ngModel)]="productoVarianteId" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Almacén ID</mat-label><input matInput type="number" min="1" name="almacenId" [(ngModel)]="almacenId" /></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Ubicación ID</mat-label><input matInput type="number" min="1" name="ubicacionAlmacenId" [(ngModel)]="ubicacionAlmacenId" /></mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Estado de stock</mat-label>
          <mat-select name="estadoStock" [(ngModel)]="estadoStock">
            <mat-option value="">Todos</mat-option><mat-option value="bajo">Bajo mínimo</mat-option><mat-option value="agotado">Agotado</mat-option>
          </mat-select>
        </mat-form-field>
        <div class="filter-actions"><button mat-flat-button color="primary" type="submit" [disabled]="loading()">Aplicar</button><button mat-button type="button" (click)="limpiarFiltros()" [disabled]="loading()">Limpiar</button></div>
      </form>

      <div class="feedback error" *ngIf="error()" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error() }}</span><button mat-button type="button" (click)="cargar()">Reintentar</button></div>
      <div class="loading" *ngIf="loading()" aria-live="polite"><mat-spinner diameter="36"></mat-spinner><span>Cargando existencias…</span></div>

      <ng-container *ngIf="!loading() && !error()">
        <div class="empty" *ngIf="existencias().length === 0">
          <mat-icon>warehouse</mat-icon><h2>No hay existencias para los filtros seleccionados</h2><p>Modifica los filtros o registra una existencia para la combinación variante/almacén/ubicación.</p>
          <button *ngIf="puedeCrear()" mat-flat-button color="primary" type="button" (click)="nueva()">Crear existencia</button>
        </div>
        <div class="table-shell" *ngIf="existencias().length > 0">
          <table>
            <caption class="sr-only">Existencias de inventario por variante</caption>
            <thead><tr><th>Producto / variante</th><th>Almacén / ubicación</th><th>Físico</th><th>Reservado</th><th>Disponible</th><th>Tránsito</th><th>Mín.</th><th>Máx.</th><th>Estado</th><th *ngIf="puedeEditar()">Acciones</th></tr></thead>
            <tbody>
              <tr *ngFor="let item of existencias(); trackBy: trackById">
                <td data-label="Producto / variante"><strong>{{ item.productoNombre }}</strong><small>{{ item.varianteSku || ('#' + item.productoVarianteId) }}</small></td>
                <td data-label="Almacén / ubicación"><strong>{{ item.almacenCodigo }} · {{ item.almacenNombre }}</strong><small>{{ item.ubicacionCodigo ? item.ubicacionCodigo + ' · ' : '' }}{{ item.ubicacionNombre || 'Raíz de almacén' }}</small></td>
                <td data-label="Físico">{{ item.stockFisico }}</td><td data-label="Reservado">{{ item.stockReservado }}</td><td data-label="Disponible"><strong>{{ item.stockDisponible }}</strong></td><td data-label="Tránsito">{{ item.stockTransito }}</td><td data-label="Mín.">{{ item.stockMinimo }}</td><td data-label="Máx.">{{ item.stockMaximo ?? '—' }}</td>
                <td data-label="Estado"><span class="status agotado" *ngIf="item.estaAgotada">Agotado</span><span class="status bajo" *ngIf="!item.estaAgotada && item.tieneStockBajo">Bajo mínimo</span><span class="status ok" *ngIf="!item.estaAgotada && !item.tieneStockBajo">Disponible</span></td>
                <td *ngIf="puedeEditar()" data-label="Acciones" class="row-actions">
                  <button mat-button color="primary" type="button" (click)="editar(item)"><mat-icon>tune</mat-icon> Configurar</button>
                  <button mat-button type="button" (click)="trazabilidad(item)"><mat-icon>qr_code_2</mat-icon> Trazabilidad</button>
                </td>
              </tr>
            </tbody>
          </table>
          <mat-paginator [length]="totalCount()" [pageIndex]="page - 1" [pageSize]="pageSize" [pageSizeOptions]="[10,25,50,100]" showFirstLastButtons (page)="onPageChange($event)" aria-label="Paginación de existencias"></mat-paginator>
        </div>
      </ng-container>
    </section>
  `,
  styles: [`
    :host{display:block}.page{padding:24px;max-width:1500px;margin:0 auto}.header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;margin-bottom:24px}.header-actions{display:flex;gap:8px;flex-wrap:wrap}.eyebrow{margin:0 0 4px;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.08em;opacity:.65}h1{margin:0;font-size:clamp(24px,3vw,34px)}.subtitle{margin:6px 0 0;opacity:.72}.filters{display:grid;grid-template-columns:repeat(4,minmax(150px,1fr)) auto;gap:12px;align-items:start;margin-bottom:18px}.filter-actions{display:flex;gap:8px;min-height:56px;align-items:center}.feedback,.loading,.empty{display:flex;align-items:center;justify-content:center;gap:10px;padding:28px;border-radius:12px}.feedback.error{justify-content:flex-start;border:1px solid rgba(244,67,54,.32);background:rgba(244,67,54,.06)}.empty{min-height:220px;flex-direction:column;text-align:center;border:1px dashed rgba(127,127,127,.35)}.empty h2,.empty p{margin:0}.table-shell{overflow-x:auto;border:1px solid rgba(127,127,127,.22);border-radius:12px}table{width:100%;border-collapse:collapse;min-width:1200px}th,td{padding:14px 16px;text-align:left;border-bottom:1px solid rgba(127,127,127,.16);vertical-align:middle}th{font-size:12px;text-transform:uppercase;letter-spacing:.04em;opacity:.72}td small{display:block;margin-top:3px;opacity:.65}.row-actions{display:flex;align-items:center;gap:2px;white-space:nowrap}.status{display:inline-flex;padding:4px 9px;border-radius:999px;font-size:12px;font-weight:700}.status.ok{background:rgba(46,125,50,.14)}.status.bajo{background:rgba(245,124,0,.14)}.status.agotado{background:rgba(198,40,40,.14)}.sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}@media(max-width:1050px){.filters{grid-template-columns:repeat(2,minmax(0,1fr))}.filter-actions{grid-column:span 2}}@media(max-width:640px){.page{padding:16px}.header{flex-direction:column}.header-actions{width:100%}.filters{grid-template-columns:1fr}.filter-actions{grid-column:auto}}
  `]
})
export class ExistenciasVarianteListComponent implements OnInit {
  readonly existencias = signal<ExistenciaVariante[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  productoVarianteId: number | null = null;
  almacenId: number | null = null;
  ubicacionAlmacenId: number | null = null;
  estadoStock: '' | 'bajo' | 'agotado' = '';
  page = 1;
  pageSize = 25;

  constructor(private readonly service: ExistenciaVarianteService, private readonly permisosRuntime: PermisosRuntimeService, private readonly router: Router) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Inventario','Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Inventario','Editar'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true); this.error.set('');
    const filtro: ExistenciaVarianteFiltro = { page:this.page, pageSize:this.pageSize, productoVarianteId:this.positivo(this.productoVarianteId), almacenId:this.positivo(this.almacenId), ubicacionAlmacenId:this.positivo(this.ubicacionAlmacenId), stockBajo:this.estadoStock==='bajo'?true:undefined, agotada:this.estadoStock==='agotado'?true:undefined, sortBy:'ProductoVarianteId', sortDirection:'asc' };
    this.service.getPaged(filtro).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => {
        if (!response.success) { this.existencias.set([]); this.totalCount.set(0); this.error.set(response.message || 'No fue posible cargar las existencias.'); return; }
        this.existencias.set(response.data.items); this.totalCount.set(response.data.totalCount); this.page=response.data.page; this.pageSize=response.data.pageSize;
      },
      error: err => { this.existencias.set([]); this.totalCount.set(0); this.error.set(err?.error?.message || err?.error?.title || 'No fue posible cargar las existencias.'); }
    });
  }

  nueva(): void { if (this.puedeCrear()) void this.router.navigate(['/inventario/existencias/nueva']); }
  editar(item: ExistenciaVariante): void { if (this.puedeEditar()) void this.router.navigate(['/inventario/existencias', item.id, 'editar']); }
  trazabilidad(item: ExistenciaVariante): void { if (this.puedeEditar()) void this.router.navigate(['/inventario/trazabilidad', item.productoVarianteId]); }
  aplicarFiltros(): void { this.page=1; this.cargar(); }
  limpiarFiltros(): void { this.productoVarianteId=null; this.almacenId=null; this.ubicacionAlmacenId=null; this.estadoStock=''; this.page=1; this.cargar(); }
  onPageChange(event: PageEvent): void { this.page=event.pageIndex+1; this.pageSize=event.pageSize; this.cargar(); }
  trackById(_: number,item: ExistenciaVariante): number { return item.id; }
  private positivo(value: number|null): number|undefined { return value && value>0 ? value : undefined; }
}
