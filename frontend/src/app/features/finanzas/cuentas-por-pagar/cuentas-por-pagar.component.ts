import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../../core/auth/permisos-runtime.service';
import {
  CondicionPagoProveedor,
  CuentaPorPagarDto,
  CuentaPorPagarFiltroDto,
  EstadoCuentaPorPagar,
  TipoAplicacionCuentaPorPagar
} from '../../../core/models/cuenta-por-pagar.model';
import { CuentasPorPagarService } from '../../../core/services/cuentas-por-pagar.service';
import { AppAlertService } from '../../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-cuentas-por-pagar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule
  ],
  template: `
    <section class="cxp-page" aria-labelledby="cxp-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Finanzas · Cuentas por pagar</p>
          <h1 id="cxp-title">Cuentas por pagar</h1>
          <p class="subtitle">Control de obligaciones, vencimientos, aplicaciones y saldo por factura de proveedor.</p>
        </div>
        <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading() || !puedeVer()">
          <mat-icon>refresh</mat-icon> Actualizar
        </button>
      </header>
    
      @if (puedeCrear()) {
        <mat-card class="panel">
          <mat-card-header><mat-card-title>Generar obligación</mat-card-title></mat-card-header>
          <mat-card-content class="form-grid">
            <mat-form-field appearance="outline">
              <mat-label>Factura proveedor ID</mat-label>
              <input matInput type="number" min="1" [(ngModel)]="generacion.facturaProveedorId" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Condición</mat-label>
              <mat-select [(ngModel)]="generacion.condicionPago">
                <mat-option [value]="1">Contado</mat-option>
                <mat-option [value]="2">Crédito</mat-option>
              </mat-select>
            </mat-form-field>
            @if (generacion.condicionPago === 2) {
              <mat-form-field appearance="outline">
                <mat-label>Vencimiento</mat-label>
                <input matInput type="date" [(ngModel)]="generacion.fechaVencimiento" />
              </mat-form-field>
            }
            <div class="form-action">
              <button mat-flat-button color="primary" type="button" (click)="generar()" [disabled]="generating()">
                <mat-icon>add_card</mat-icon> Generar
              </button>
            </div>
          </mat-card-content>
        </mat-card>
      }
    
      <mat-card class="panel">
        <mat-card-content class="filters">
          <mat-form-field appearance="outline">
            <mat-label>Estado</mat-label>
            <mat-select [(ngModel)]="filtro.estado">
              <mat-option [value]="null">Todos</mat-option>
              <mat-option [value]="1">Pendiente</mat-option>
              <mat-option [value]="2">Parcial</mat-option>
              <mat-option [value]="3">Pagada</mat-option>
              <mat-option [value]="4">Anulada</mat-option>
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Condición</mat-label>
            <mat-select [(ngModel)]="filtro.condicionPago">
              <mat-option [value]="null">Todas</mat-option>
              <mat-option [value]="1">Contado</mat-option>
              <mat-option [value]="2">Crédito</mat-option>
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Proveedor ID</mat-label>
            <input matInput type="number" min="1" [(ngModel)]="filtro.proveedorId" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Moneda</mat-label>
            <input matInput maxlength="3" [(ngModel)]="filtro.moneda" placeholder="HNL" />
          </mat-form-field>
          <button mat-flat-button color="primary" type="button" (click)="aplicarFiltros()" [disabled]="loading() || !puedeVer()">
            <mat-icon>filter_alt</mat-icon> Filtrar
          </button>
        </mat-card-content>
      </mat-card>
    
      @if (loading()) {
        <div class="loading" role="status" aria-live="polite">
          <mat-spinner diameter="38"></mat-spinner><span>Cargando cuentas por pagar…</span>
        </div>
      }
    
      @if (!loading()) {
        <mat-card class="panel">
          <mat-card-content>
            @if (items().length) {
              <div class="table-wrap">
                <table mat-table [dataSource]="items()">
                  <ng-container matColumnDef="id"><th mat-header-cell *matHeaderCellDef>ID</th><td mat-cell *matCellDef="let row">{{ row.id }}</td></ng-container>
                  <ng-container matColumnDef="factura"><th mat-header-cell *matHeaderCellDef>Factura</th><td mat-cell *matCellDef="let row">#{{ row.facturaProveedorId }}</td></ng-container>
                  <ng-container matColumnDef="proveedor"><th mat-header-cell *matHeaderCellDef>Proveedor</th><td mat-cell *matCellDef="let row">#{{ row.proveedorId }}</td></ng-container>
                  <ng-container matColumnDef="vencimiento"><th mat-header-cell *matHeaderCellDef>Vencimiento</th><td mat-cell *matCellDef="let row">{{ row.fechaVencimientoUtc | date:'mediumDate' }}</td></ng-container>
                  <ng-container matColumnDef="condicion"><th mat-header-cell *matHeaderCellDef>Condición</th><td mat-cell *matCellDef="let row">{{ condicionLabel(row.condicionPago) }}</td></ng-container>
                  <ng-container matColumnDef="estado"><th mat-header-cell *matHeaderCellDef>Estado</th><td mat-cell *matCellDef="let row"><span class="status" [attr.data-status]="row.estado">{{ estadoLabel(row.estado) }}</span></td></ng-container>
                  <ng-container matColumnDef="saldo"><th mat-header-cell *matHeaderCellDef>Saldo</th><td mat-cell *matCellDef="let row">{{ row.saldo | number:'1.2-2' }} {{ row.moneda }}</td></ng-container>
                  <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef>Acciones</th><td mat-cell *matCellDef="let row"><button mat-icon-button type="button" (click)="seleccionar(row.id)" aria-label="Ver detalle de cuenta por pagar"><mat-icon>visibility</mat-icon></button></td></ng-container>
                  <tr mat-header-row *matHeaderRowDef="columns"></tr>
                  <tr mat-row *matRowDef="let row; columns: columns"></tr>
                </table>
              </div>
            } @else {
              <div class="empty" role="status">No hay cuentas por pagar para los filtros seleccionados.</div>
            }
            <mat-paginator [length]="total()" [pageIndex]="filtro.page - 1" [pageSize]="filtro.pageSize" [pageSizeOptions]="[10,20,50]" showFirstLastButtons (page)="cambiarPagina($event)"></mat-paginator>
          </mat-card-content>
        </mat-card>
      }
    
      @if (seleccionada(); as cuenta) {
        <mat-card class="panel detail">
          <mat-card-header>
            <mat-card-title>Cuenta #{{ cuenta.id }} · {{ estadoLabel(cuenta.estado) }}</mat-card-title>
            <mat-card-subtitle>Factura #{{ cuenta.facturaProveedorId }} · Proveedor #{{ cuenta.proveedorId }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="summary-grid">
              <div><span>Condición</span><strong>{{ condicionLabel(cuenta.condicionPago) }}</strong></div>
              <div><span>Vencimiento</span><strong>{{ cuenta.fechaVencimientoUtc | date:'mediumDate' }}</strong></div>
              <div><span>Original</span><strong>{{ cuenta.montoOriginal | number:'1.2-2' }} {{ cuenta.moneda }}</strong></div>
              <div><span>Aplicado</span><strong>{{ cuenta.montoAplicado | number:'1.2-2' }} {{ cuenta.moneda }}</strong></div>
              <div><span>Saldo</span><strong>{{ cuenta.saldo | number:'1.2-2' }} {{ cuenta.moneda }}</strong></div>
            </div>
            @if (puedeEditar() && cuenta.estado !== 3 && cuenta.estado !== 4) {
              <div class="apply-form">
                <h3>Registrar aplicación</h3>
                <mat-form-field appearance="outline">
                  <mat-label>Tipo</mat-label>
                  <mat-select [(ngModel)]="aplicacion.tipo">
                    <mat-option [value]="1">Pago</mat-option><mat-option [value]="2">Anticipo</mat-option><mat-option [value]="3">Retención</mat-option><mat-option [value]="4">Nota de crédito</mat-option>
                  </mat-select>
                </mat-form-field>
                <mat-form-field appearance="outline"><mat-label>Monto</mat-label><input matInput type="number" min="0.0001" step="0.01" [(ngModel)]="aplicacion.monto" /></mat-form-field>
                <mat-form-field appearance="outline"><mat-label>Referencia</mat-label><input matInput maxlength="200" [(ngModel)]="aplicacion.referenciaExterna" /></mat-form-field>
                <button mat-flat-button color="primary" type="button" (click)="registrarAplicacion()" [disabled]="applying()">Aplicar</button>
              </div>
            }
            <h3>Aplicaciones</h3>
            @if (cuenta.aplicaciones.length) {
              <div class="applications">
                @for (app of cuenta.aplicaciones; track app) {
                  <div class="application">
                    <div><strong>{{ tipoAplicacionLabel(app.tipo) }}</strong><span>{{ app.monto | number:'1.2-2' }} {{ cuenta.moneda }}</span></div>
                    <div><span>{{ app.fechaAplicacionUtc | date:'short' }}</span><span>{{ app.referenciaExterna || 'Sin referencia' }}</span></div>
                    <span class="status">{{ app.revertida ? 'Revertida' : 'Activa' }}</span>
                    @if (!app.revertida && puedeEditar() && cuenta.estado !== 4) {
                      <button mat-stroked-button color="warn" type="button" (click)="revertir(app.idempotencyKey)">Revertir</button>
                    }
                  </div>
                }
              </div>
            } @else {
              <div class="empty">Sin aplicaciones registradas.</div>
            }
          </mat-card-content>
          <mat-card-actions align="end">
            @if (puedeAnular() && cuenta.estado !== 4) {
              <button mat-stroked-button color="warn" type="button" (click)="anular()"><mat-icon>block</mat-icon> Anular cuenta</button>
            }
          </mat-card-actions>
        </mat-card>
      }
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .cxp-page{display:grid;gap:16px;padding:20px;max-width:1500px;margin:auto}.page-header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.eyebrow{margin:0;color:var(--color-primary);font-weight:700}.subtitle{margin:4px 0 0;color:var(--color-text-muted)}h1{margin:2px 0}.panel{border:1px solid var(--color-border)}.form-grid,.filters,.apply-form{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px;align-items:center}.form-action{align-self:stretch;display:flex;align-items:center}.table-wrap{overflow:auto}table{width:100%;min-width:850px}.loading{min-height:160px;display:grid;place-items:center;gap:10px}.empty{padding:24px;text-align:center;color:var(--color-text-muted)}.status{display:inline-flex;padding:4px 9px;border-radius:999px;background:var(--color-bg);font-weight:650}.status[data-status='1']{color:#8a4b00}.status[data-status='2']{color:#075985}.status[data-status='3']{color:#166534}.status[data-status='4']{color:#991b1b}.summary-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px;margin-bottom:20px}.summary-grid div{display:grid;gap:4px;padding:12px;border:1px solid var(--color-border);border-radius:8px}.summary-grid span{color:var(--color-text-muted);font-size:.86rem}.apply-form{margin:16px 0;padding:16px;background:var(--color-bg);border-radius:10px}.apply-form h3{grid-column:1/-1;margin:0}.applications{display:grid;gap:10px}.application{display:grid;grid-template-columns:2fr 2fr auto auto;gap:12px;align-items:center;padding:12px;border:1px solid var(--color-border);border-radius:8px}.application>div{display:flex;gap:8px;flex-wrap:wrap}@media(max-width:760px){.page-header{display:grid}.application{grid-template-columns:1fr}.cxp-page{padding:12px}}
  `]
})
export class CuentasPorPagarComponent implements OnInit {
  private readonly service = inject(CuentasPorPagarService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly alertas = inject(AppAlertService);

  readonly loading = signal(false);
  readonly generating = signal(false);
  readonly applying = signal(false);
  readonly items = signal<CuentaPorPagarDto[]>([]);
  readonly total = signal(0);
  readonly seleccionada = signal<CuentaPorPagarDto | null>(null);

  readonly columns = ['id','factura','proveedor','vencimiento','condicion','estado','saldo','acciones'];
  filtro: CuentaPorPagarFiltroDto = { page: 1, pageSize: 20, sortDirection: 'asc' };
  generacion: { facturaProveedorId: number | null; condicionPago: CondicionPagoProveedor; fechaVencimiento: string } = { facturaProveedorId: null, condicionPago: 2, fechaVencimiento: '' };
  aplicacion: { tipo: TipoAplicacionCuentaPorPagar; monto: number | null; referenciaExterna: string } = { tipo: 1, monto: null, referenciaExterna: '' };

  ngOnInit(): void { this.cargar(); }
  puedeVer(): boolean { return this.permisos.puede('Finanzas','Ver'); }
  puedeCrear(): boolean { return this.permisos.puede('Finanzas','Crear'); }
  puedeEditar(): boolean { return this.permisos.puede('Finanzas','Editar'); }
  puedeAnular(): boolean { return this.permisos.puede('Finanzas','Anular'); }

  cargar(): void {
    if (!this.puedeVer()) return;
    this.loading.set(true);
    this.service.buscar(this.filtro).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: res => { if (res.success) { this.items.set(res.data.items); this.total.set(res.data.totalCount); } else this.notificar('No se pudieron cargar las cuentas por pagar.'); },
      error: () => this.notificar('No se pudo consultar Cuentas por Pagar. Intente nuevamente.')
    });
  }

  aplicarFiltros(): void { this.filtro.page = 1; this.cargar(); }
  cambiarPagina(event: PageEvent): void { this.filtro.page = event.pageIndex + 1; this.filtro.pageSize = event.pageSize; this.cargar(); }

  seleccionar(id: number): void {
    this.service.getById(id).subscribe({
      next: res => res.success ? this.seleccionada.set(res.data) : this.notificar('No se pudo cargar el detalle.'),
      error: () => this.notificar('No se pudo cargar el detalle de la cuenta por pagar.')
    });
  }

  generar(): void {
    if (!this.puedeCrear() || !this.generacion.facturaProveedorId || this.generacion.facturaProveedorId < 1) return;
    if (this.generacion.condicionPago === 2 && !this.generacion.fechaVencimiento) { this.notificar('La cuenta a crédito requiere fecha de vencimiento.'); return; }
    this.generating.set(true);
    const fecha = this.generacion.condicionPago === 2 ? this.aUtc(this.generacion.fechaVencimiento) : null;
    this.service.generar({ facturaProveedorId: this.generacion.facturaProveedorId, condicionPago: this.generacion.condicionPago, fechaVencimientoUtc: fecha }).pipe(finalize(() => this.generating.set(false))).subscribe({
      next: res => { if (res.success) { this.seleccionada.set(res.data); this.cargar(); } else this.notificar('No se pudo generar la cuenta por pagar.'); },
      error: () => this.notificar('No se pudo generar la cuenta por pagar. Verifique la factura y la condición de pago.')
    });
  }

  registrarAplicacion(): void {
    const cuenta = this.seleccionada();
    if (!cuenta || !this.puedeEditar() || !this.aplicacion.monto || this.aplicacion.monto <= 0) return;
    this.applying.set(true);
    this.service.aplicar(cuenta.id, { tipo: this.aplicacion.tipo, monto: this.aplicacion.monto, idempotencyKey: this.nuevaIdempotencia(), referenciaExterna: this.aplicacion.referenciaExterna.trim() || null, fechaAplicacionUtc: new Date().toISOString() }).pipe(finalize(() => this.applying.set(false))).subscribe({
      next: res => { if (res.success) { this.seleccionada.set(res.data); this.aplicacion = { tipo: 1, monto: null, referenciaExterna: '' }; this.cargar(); } else this.notificar('No se pudo registrar la aplicación.'); },
      error: () => this.notificar('La aplicación fue rechazada. Verifique monto, saldo, moneda y estado.')
    });
  }

  async revertir(idempotencyKey: string): Promise<void> {
    const cuenta = this.seleccionada();
    if (!cuenta || !this.puedeEditar()) return;
    const motivo = await this.alertas.solicitarTexto({ titulo: 'Revertir aplicación', mensaje: 'Confirme el motivo de la reversión.', tipo: 'advertencia', entrada: { etiqueta: 'Motivo', requerida: true }, confirmarTexto: 'Revertir' });
    if (!motivo) return;
    this.service.revertirAplicacion(cuenta.id, { idempotencyKey, motivo, fechaReversionUtc: new Date().toISOString() }).subscribe({ next: res => { if (res.success) { this.seleccionada.set(res.data); this.cargar(); } else this.notificar('No se pudo revertir la aplicación.'); }, error: () => this.notificar('No se pudo revertir la aplicación.') });
  }

  async anular(): Promise<void> {
    const cuenta = this.seleccionada();
    if (!cuenta || !this.puedeAnular() || cuenta.estado === 4) return;
    const motivo = await this.alertas.solicitarTexto({ titulo: 'Anular cuenta por pagar', mensaje: 'Esta operación requiere un motivo explícito.', tipo: 'peligro', entrada: { etiqueta: 'Motivo', requerida: true }, confirmarTexto: 'Anular' });
    if (!motivo) return;
    this.service.anular(cuenta.id, { motivo, fechaAnulacionUtc: new Date().toISOString() }).subscribe({ next: res => { if (res.success) { this.seleccionada.set(res.data); this.cargar(); } else this.notificar('No se pudo anular la cuenta por pagar.'); }, error: () => this.notificar('No se pudo anular la cuenta por pagar.') });
  }

  estadoLabel(value: EstadoCuentaPorPagar): string { return ({1:'Pendiente',2:'Parcial',3:'Pagada',4:'Anulada'} as const)[value] ?? 'Desconocido'; }
  condicionLabel(value: CondicionPagoProveedor): string { return value === 1 ? 'Contado' : value === 2 ? 'Crédito' : 'Desconocida'; }
  tipoAplicacionLabel(value: TipoAplicacionCuentaPorPagar): string { return ({1:'Pago',2:'Anticipo',3:'Retención',4:'Nota de crédito'} as const)[value] ?? 'Desconocida'; }
  private aUtc(date: string): string { return new Date(`${date}T00:00:00Z`).toISOString(); }
  private nuevaIdempotencia(): string { return globalThis.crypto?.randomUUID?.() ?? `cxp-${Date.now()}-${Math.random().toString(36).slice(2)}`; }
  private notificar(mensaje: string): void { void this.alertas.confirmar({ titulo: 'Cuentas por pagar', mensaje, tipo: 'advertencia', confirmarTexto: 'Aceptar', cancelarTexto: 'Cerrar' }); }
}
