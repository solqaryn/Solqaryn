import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Factura } from '../../core/models/factura.model';
import { FacturaService } from '../../services/factura.service';

@Component({
  selector: 'app-cuentas-por-cobrar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page" aria-labelledby="cxc-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Facturación</p>
          <h1 id="cxc-title">Cuentas por cobrar</h1>
          <p class="subtitle">Facturas vigentes con saldo pendiente, ordenadas por vencimiento.</p>
        </div>
        <button type="button" class="secondary" (click)="cargar()" [disabled]="loading()">
          Actualizar
        </button>
      </header>
    
      <div class="summary" aria-live="polite">
        <div class="summary-card">
          <span>Documentos pendientes</span>
          <strong>{{ cuentas().length }}</strong>
        </div>
        <div class="summary-card">
          <span>Saldo pendiente</span>
          <strong>HNL {{ totalPendiente() | number:'1.2-2' }}</strong>
        </div>
        <div class="summary-card">
          <span>Vencidos</span>
          <strong>{{ totalVencidos() }}</strong>
        </div>
      </div>
    
      @if (loading()) {
        <div class="state" role="status" aria-live="polite">
          <div class="spinner" aria-hidden="true"></div>
          <span>Cargando cuentas por cobrar…</span>
        </div>
      }
    
      @if (!loading() && errorMessage()) {
        <div class="state error" role="alert">
          <strong>No fue posible cargar las cuentas por cobrar.</strong>
          <span>{{ errorMessage() }}</span>
          <button type="button" class="primary" (click)="cargar()">Reintentar</button>
        </div>
      }
    
      @if (!loading() && !errorMessage() && cuentas().length === 0) {
        <div class="state empty">
          <strong>No hay saldos pendientes.</strong>
          <span>Las facturas activas están al día.</span>
        </div>
      }
    
      @if (!loading() && !errorMessage() && cuentas().length > 0) {
        <div class="table-shell">
          <table>
            <thead>
              <tr>
                <th>Factura</th>
                <th>Venta</th>
                <th>Cliente</th>
                <th>Vencimiento</th>
                <th>Estado</th>
                <th class="numeric">Total</th>
                <th class="numeric">Pagado</th>
                <th class="numeric">Saldo</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (cuenta of cuentas(); track trackByFactura($index, cuenta)) {
                <tr>
                  <td data-label="Factura">
                    <a class="document-link" [routerLink]="['/facturas', cuenta.id]">{{ cuenta.numeroFactura }}</a>
                  </td>
                  <td data-label="Venta">
                    <a [routerLink]="['/ventas', cuenta.ventaId]">{{ cuenta.numeroVentaOrigen || ('#' + cuenta.ventaId) }}</a>
                  </td>
                  <td data-label="Cliente">{{ cuenta.clienteNombre }}</td>
                  <td data-label="Vencimiento">
                    <span [class.overdue]="estaVencida(cuenta)">
                      {{ cuenta.fechaVencimiento ? (cuenta.fechaVencimiento | date:'dd/MM/yyyy') : 'Sin fecha' }}
                    </span>
                  </td>
                  <td data-label="Estado"><span class="status">{{ cuenta.estadoPago || cuenta.estado }}</span></td>
                  <td data-label="Total" class="numeric">{{ cuenta.moneda }} {{ cuenta.total | number:'1.2-2' }}</td>
                  <td data-label="Pagado" class="numeric">{{ cuenta.moneda }} {{ cuenta.totalPagado | number:'1.2-2' }}</td>
                  <td data-label="Saldo" class="numeric balance">{{ cuenta.moneda }} {{ cuenta.saldoPendiente | number:'1.2-2' }}</td>
                  <td data-label="Acciones" class="actions">
                    <a class="secondary link-button" [routerLink]="['/facturas', cuenta.id]">Ver factura</a>
                    <a class="primary link-button" [routerLink]="['/facturas', cuenta.id, 'pagos']">Pagos</a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display: block; }
    .page { display: grid; gap: 1.25rem; }
    .page-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
    .eyebrow { margin: 0 0 .25rem; font-size: .78rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; opacity: .65; }
    h1 { margin: 0; font-size: clamp(1.65rem, 2.2vw, 2.25rem); }
    .subtitle { margin: .4rem 0 0; opacity: .72; }
    .summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: .85rem; }
    .summary-card, .table-shell, .state { border: 1px solid rgba(127, 127, 127, .25); border-radius: 14px; background: var(--mat-sys-surface, rgba(127,127,127,.04)); }
    .summary-card { padding: 1rem; display: grid; gap: .35rem; }
    .summary-card span { font-size: .82rem; opacity: .7; }
    .summary-card strong { font-size: 1.35rem; }
    .state { min-height: 180px; padding: 2rem; display: grid; place-items: center; align-content: center; gap: .75rem; text-align: center; }
    .state.error { border-color: rgba(190, 30, 45, .45); }
    .spinner { width: 32px; height: 32px; border: 3px solid rgba(127,127,127,.25); border-top-color: currentColor; border-radius: 50%; animation: spin .8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .table-shell { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; min-width: 980px; }
    th, td { padding: .85rem .9rem; border-bottom: 1px solid rgba(127,127,127,.18); text-align: left; vertical-align: middle; }
    th { font-size: .76rem; text-transform: uppercase; letter-spacing: .04em; opacity: .72; }
    tbody tr:last-child td { border-bottom: 0; }
    a { color: inherit; }
    .document-link { font-weight: 700; }
    .numeric { text-align: right; white-space: nowrap; }
    .balance { font-weight: 800; }
    .overdue { font-weight: 700; text-decoration: underline; text-decoration-style: dotted; }
    .status { display: inline-flex; padding: .25rem .55rem; border-radius: 999px; background: rgba(127,127,127,.14); font-size: .78rem; font-weight: 650; }
    .actions { display: flex; gap: .45rem; white-space: nowrap; }
    button, .link-button { border: 0; border-radius: 9px; padding: .58rem .78rem; font: inherit; font-weight: 650; cursor: pointer; text-decoration: none; display: inline-flex; align-items: center; justify-content: center; }
    button:disabled { cursor: not-allowed; opacity: .55; }
    .primary { background: var(--mat-sys-primary, #1f5eff); color: var(--mat-sys-on-primary, #fff); }
    .secondary { background: rgba(127,127,127,.14); color: inherit; }
    @media (max-width: 760px) {
      .summary { grid-template-columns: 1fr; }
      .table-shell { border: 0; overflow: visible; background: transparent; }
      table, thead, tbody, tr, th, td { display: block; min-width: 0; }
      thead { display: none; }
      tbody { display: grid; gap: .8rem; }
      tr { border: 1px solid rgba(127,127,127,.25); border-radius: 12px; padding: .55rem; }
      td { display: grid; grid-template-columns: 110px 1fr; gap: .75rem; padding: .55rem; text-align: left !important; }
      td::before { content: attr(data-label); font-size: .75rem; font-weight: 700; opacity: .65; text-transform: uppercase; }
      .actions { white-space: normal; }
    }
  `]
})
export class CuentasPorCobrarComponent implements OnInit {
  private readonly facturaService = inject(FacturaService);

  readonly cuentas = signal<Factura[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly totalPendiente = computed(() => this.cuentas().reduce((sum, cuenta) => sum + cuenta.saldoPendiente, 0));
  readonly totalVencidos = computed(() => this.cuentas().filter(cuenta => this.estaVencida(cuenta)).length);

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.facturaService.getCuentasPorCobrar()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this.cuentas.set([]);
            this.errorMessage.set(response.message || 'La API rechazó la consulta.');
            return;
          }
          this.cuentas.set(response.data ?? []);
        },
        error: (error: HttpErrorResponse) => {
          this.cuentas.set([]);
          this.errorMessage.set(this.extractError(error));
        }
      });
  }

  trackByFactura(_: number, cuenta: Factura): number {
    return cuenta.id;
  }

  estaVencida(cuenta: Factura): boolean {
    if (!cuenta.fechaVencimiento || cuenta.saldoPendiente <= 0) return false;
    const vencimiento = new Date(cuenta.fechaVencimiento);
    if (Number.isNaN(vencimiento.getTime())) return false;
    const hoy = new Date();
    hoy.setHours(0, 0, 0, 0);
    vencimiento.setHours(0, 0, 0, 0);
    return vencimiento.getTime() < hoy.getTime();
  }

  private extractError(error: HttpErrorResponse): string {
    const apiMessage = error.error?.message;
    if (typeof apiMessage === 'string' && apiMessage.trim()) return apiMessage;
    if (error.status === 403) return 'No tiene permiso Facturacion/Ver para consultar esta información.';
    if (error.status === 401) return 'La sesión no está autorizada para consultar esta información.';
    return 'Ocurrió un error al consultar el endpoint /cuentas-por-cobrar.';
  }
}
