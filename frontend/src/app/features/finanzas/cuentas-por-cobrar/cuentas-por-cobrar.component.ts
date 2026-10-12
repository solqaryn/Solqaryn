import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, Injectable, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Observable, finalize } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Factura } from '../../../core/models/factura.model';

@Injectable({ providedIn: 'root' })
export class CuentasPorCobrarService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/cuentas-por-cobrar`;

  listar(): Observable<ApiResponse<Factura[]>> {
    return this.http.get<ApiResponse<Factura[]>>(this.baseUrl);
  }
}

@Component({
  selector: 'app-cuentas-por-cobrar',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule, MatTableModule],
  template: `
    <section class="cxc-page" aria-labelledby="cxc-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Facturación · Cuentas por cobrar</p>
          <h1 id="cxc-title">Cuentas por cobrar</h1>
          <p class="subtitle">Proyección de facturas con saldo pendiente. Esta vista es únicamente de consulta.</p>
        </div>
        <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading()">
          <mat-icon>refresh</mat-icon>
          Actualizar
        </button>
      </header>

      @if (loading()) {
        <div class="state" role="status" aria-live="polite">
          <mat-spinner diameter="40"></mat-spinner>
          <span>Cargando cuentas por cobrar…</span>
        </div>
      } @else if (error()) {
        <mat-card class="state error" role="alert">
          <mat-card-content>
            <mat-icon aria-hidden="true">error_outline</mat-icon>
            <div>
              <strong>No fue posible cargar las cuentas por cobrar.</strong>
              <p>{{ error() }}</p>
            </div>
            <button mat-stroked-button type="button" (click)="cargar()">Reintentar</button>
          </mat-card-content>
        </mat-card>
      } @else if (!items().length) {
        <mat-card class="state empty" role="status" aria-live="polite">
          <mat-card-content>
            <mat-icon aria-hidden="true">check_circle_outline</mat-icon>
            <div>
              <strong>Sin saldos pendientes</strong>
              <p>No existen facturas por cobrar en este momento.</p>
            </div>
          </mat-card-content>
        </mat-card>
      } @else {
        <mat-card class="panel">
          <mat-card-content>
            <div class="table-wrap" tabindex="0" aria-label="Listado de cuentas por cobrar">
              <table mat-table [dataSource]="items()">
                <ng-container matColumnDef="numero">
                  <th mat-header-cell *matHeaderCellDef>Factura</th>
                  <td mat-cell *matCellDef="let row">{{ row.numeroFactura }}</td>
                </ng-container>
                <ng-container matColumnDef="cliente">
                  <th mat-header-cell *matHeaderCellDef>Cliente</th>
                  <td mat-cell *matCellDef="let row">{{ row.clienteNombre }}</td>
                </ng-container>
                <ng-container matColumnDef="vencimiento">
                  <th mat-header-cell *matHeaderCellDef>Vencimiento</th>
                  <td mat-cell *matCellDef="let row">{{ row.fechaVencimiento ? (row.fechaVencimiento | date:'mediumDate') : 'Sin fecha' }}</td>
                </ng-container>
                <ng-container matColumnDef="estado">
                  <th mat-header-cell *matHeaderCellDef>Estado</th>
                  <td mat-cell *matCellDef="let row"><span class="status">{{ row.estado }}</span></td>
                </ng-container>
                <ng-container matColumnDef="total">
                  <th mat-header-cell *matHeaderCellDef>Total</th>
                  <td mat-cell *matCellDef="let row">{{ row.total | number:'1.2-2' }} {{ row.moneda }}</td>
                </ng-container>
                <ng-container matColumnDef="pagado">
                  <th mat-header-cell *matHeaderCellDef>Pagado</th>
                  <td mat-cell *matCellDef="let row">{{ row.totalPagado | number:'1.2-2' }} {{ row.moneda }}</td>
                </ng-container>
                <ng-container matColumnDef="pendiente">
                  <th mat-header-cell *matHeaderCellDef>Saldo pendiente</th>
                  <td mat-cell *matCellDef="let row"><strong>{{ row.saldoPendiente | number:'1.2-2' }} {{ row.moneda }}</strong></td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="columns"></tr>
                <tr mat-row *matRowDef="let row; columns: columns"></tr>
              </table>
            </div>
          </mat-card-content>
        </mat-card>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .cxc-page{display:grid;gap:16px;padding:20px;max-width:1500px;margin:0 auto}.page-header{display:flex;align-items:flex-start;justify-content:space-between;gap:16px}.eyebrow{margin:0;color:var(--color-primary);font-weight:700}.subtitle{margin:4px 0 0;color:var(--color-text-muted)}h1{margin:2px 0}.panel,.state{border:1px solid var(--color-border)}.state{min-height:170px;display:grid;place-items:center;text-align:center}.state mat-card-content{display:flex;align-items:center;justify-content:center;gap:12px;flex-wrap:wrap}.state p{margin:4px 0;color:var(--color-text-muted)}.error mat-icon{color:var(--color-error,#b91c1c)}.empty mat-icon{color:var(--color-success,#15803d)}.table-wrap{overflow:auto;border-radius:8px}.table-wrap:focus-visible{outline:3px solid var(--color-primary);outline-offset:2px}table{width:100%;min-width:900px}.status{display:inline-flex;padding:4px 9px;border-radius:999px;background:var(--color-bg);font-weight:650}@media(max-width:760px){.cxc-page{padding:12px}.page-header{display:grid}}
  `]
})
export class CuentasPorCobrarComponent implements OnInit {
  private readonly service = inject(CuentasPorCobrarService);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly items = signal<Factura[]>([]);
  readonly columns = ['numero', 'cliente', 'vencimiento', 'estado', 'total', 'pagado', 'pendiente'];

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.listar().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => {
        if (!response.success) {
          this.items.set([]);
          this.error.set(response.message || 'Error de API desconocido');
          return;
        }
        this.items.set(response.data ?? []);
      },
      error: (err: HttpErrorResponse) => {
        this.items.set([]);
        if (err.status === 403) {
          this.error.set('No tiene permiso Facturacion/Ver para consultar esta información.');
        } else if (err.status === 401) {
          this.error.set('La sesión no está autorizada para consultar esta información.');
        } else {
          this.error.set('Verifica tu conexión o permisos e inténtalo nuevamente.');
        }
      }
    });
  }
}
