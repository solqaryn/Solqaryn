import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Factura } from '../../core/models/factura.model';
import { FacturaService } from '../../services/factura.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-cuentas-por-cobrar',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule],
  template: `
    <section class="page" aria-labelledby="cxc-title">
      <header class="page-header">
        <div>
          
          <h1 id="cxc-title" class="page-title">Cuentas por cobrar</h1>
          <p class="subtitle page-subtitle">Consulta las facturas pendientes de cobro, sus fechas de vencimiento y los importes pagados. Accede a los pagos cuando tu perfil tenga autorización.</p>
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
        <div class="summary-card" aria-label="Saldo pendiente por moneda">
          <span>Saldo pendiente por moneda</span>
          @for (saldo of saldosPorMoneda(); track saldo.moneda) {
            <strong>{{ saldo.moneda }} {{ saldo.total | number:'1.2-2' }}</strong>
          } @empty {
            <strong>—</strong>
          }
        </div>
        <div class="summary-card">
          <span>Vencidos</span>
          <strong>{{ totalVencidos() }}</strong>
        </div>
      </div>
    
      <div class="card app-list-surface">
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
        <div class="state empty app-list-empty">
          <strong>No hay saldos pendientes.</strong>
          <p>No hay facturas vigentes con saldo pendiente.</p>
        </div>
      }
    
      @if (!loading() && !errorMessage() && cuentas().length > 0) {
        <div class="table-shell app-table-shell" role="region" aria-label="Tabla de cuentas por cobrar" tabindex="0">
          <table class="app-data-table">
            <thead>
              <tr>
                <th>Factura</th>
                <th>Venta</th>
                <th>Cliente</th>
                <th>Vencimiento</th>
                <th>Estado</th>
                <th class="numeric app-col-number">Total</th>
                <th class="numeric app-col-number">Pagado</th>
                <th class="numeric app-col-number">Saldo</th>
                <th class="actions-heading">Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (cuenta of cuentas(); track trackByFactura($index, cuenta)) {
                <tr>
                  <td data-label="Factura">
                    <a class="document-link" [routerLink]="['/facturas', cuenta.id]">{{ cuenta.numeroFactura }}</a>
                  </td>
                  <td data-label="Venta">
                    @if (puedeVerVenta()) {
                      <a [routerLink]="['/ventas', cuenta.ventaId]">{{ cuenta.numeroVentaOrigen || ('#' + cuenta.ventaId) }}</a>
                    } @else {
                      <span>{{ cuenta.numeroVentaOrigen || ('#' + cuenta.ventaId) }}</span>
                    }
                  </td>
                  <td data-label="Cliente">{{ cuenta.clienteNombre }}</td>
                  <td data-label="Vencimiento">
                    <span [class.overdue]="estaVencida(cuenta)">
                      {{ cuenta.fechaVencimiento ? (cuenta.fechaVencimiento | date:'dd/MM/yyyy') : 'Sin fecha' }}
                    </span>
                  </td>
                  <td data-label="Estado"><span class="status">{{ cuenta.estadoPago || cuenta.estado }}</span></td>
                  <td data-label="Total" class="numeric app-col-number">{{ cuenta.moneda }} {{ cuenta.total | number:'1.2-2' }}</td>
                  <td data-label="Pagado" class="numeric app-col-number">{{ cuenta.moneda }} {{ cuenta.totalPagado | number:'1.2-2' }}</td>
                  <td data-label="Saldo" class="numeric balance app-col-number">{{ cuenta.moneda }} {{ cuenta.saldoPendiente | number:'1.2-2' }}</td>
                  <td data-label="Acciones" class="actions">
                    <a class="action-view" mat-icon-button title="Ver factura" [attr.aria-label]="'Ver factura ' + cuenta.numeroFactura" [routerLink]="['/facturas', cuenta.id]"><mat-icon>visibility</mat-icon></a>
                    @if (puedeAplicarPago() && cuenta.saldoPendiente > 0) {
                      <a class="primary link-button" [routerLink]="['/facturas', cuenta.id, 'pagos']" [attr.aria-label]="'Pagos de factura ' + cuenta.numeroFactura">Pagos</a>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
      </div>
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display: block; min-width:0; }
    .page { display: grid; grid-template-columns:minmax(0,1fr); min-width:0; gap: 1.25rem; }
    .page-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
    h1 { margin: 0; font-size: clamp(1.65rem, 2.2vw, 2.25rem); }
    .subtitle { margin: .4rem 0 0; color:var(--color-text-muted); max-width:70ch; }
    .summary { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: .85rem; }
    .summary-card, .table-shell, .state { border: 1px solid var(--color-border); border-radius: var(--radius); background: var(--color-surface); }
    .summary-card { padding: 1rem; display: grid; gap: .35rem; min-width:0; align-content:start; }
    .summary-card span { font-size: .82rem; color:var(--color-text-muted); }
    .summary-card strong { font-size: 1.35rem; font-variant-numeric:tabular-nums; overflow-wrap:anywhere; }
    .state { min-height: 180px; padding: 2rem; display: grid; place-items: center; align-content: center; gap: .75rem; text-align: center; }
    .state.error { border-color: var(--color-danger); }
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
    }
  `]
})
export class CuentasPorCobrarComponent implements OnInit {
  private readonly facturaService = inject(FacturaService);
  private readonly permisos = inject(PermisosRuntimeService);
  readonly puedeVerVenta = computed(() => this.permisos.puede('Ventas', 'Ver'));
  readonly puedeAplicarPago = computed(() => this.permisos.puede('Facturacion', 'Aplicar'));

  readonly cuentas = signal<Factura[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  // No sumar monedas distintas como si fueran una sola ni realizar conversiones implícitas.
  readonly saldosPorMoneda = computed(() => {
    const saldos = new Map<string, number>();
    for (const cuenta of this.cuentas()) {
      const moneda = cuenta.moneda?.trim() || 'Moneda no indicada';
      saldos.set(moneda, (saldos.get(moneda) ?? 0) + cuenta.saldoPendiente);
    }
    return [...saldos.entries()].map(([moneda, total]) => ({ moneda, total }));
  });
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
    if (error.status === 403) return 'Tu perfil no tiene permiso para consultar cuentas por cobrar.';
    if (error.status === 401) return 'Tu sesión ya no permite consultar esta información. Vuelve a iniciar sesión.';
    const apiMessage = error.error?.message;
    if (typeof apiMessage === 'string' && apiMessage.trim()) return apiMessage;
    return 'No pudimos consultar las facturas pendientes. Verifica tu conexión e inténtalo nuevamente.';
  }
}
