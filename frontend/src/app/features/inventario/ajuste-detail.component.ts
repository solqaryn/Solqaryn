import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AjusteInventario } from '../../core/models/ajuste-inventario.model';
import { AjusteInventarioService } from '../../services/ajuste-inventario.service';

@Component({
  selector: 'app-ajuste-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <section class="detail-page" aria-labelledby="ajuste-title">
      @if (loading()) {
        <div class="loading"><mat-spinner diameter="36"></mat-spinner><span>Cargando ajuste...</span></div>
      } @else if (error()) {
        <div class="error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error() }}</span></div>
      } @else if (ajuste(); as item) {
        <header class="page-header">
          <div>
            <p class="eyebrow">Inventario empresarial</p>
            <h1 id="ajuste-title">{{ item.numeroAjuste }}</h1>
            <p class="subtitle">Detalle y trazabilidad del ajuste de inventario.</p>
          </div>
          <div class="header-actions">
            @if (item.estado === 'Borrador' && puedeEditar()) {
              <a mat-stroked-button [routerLink]="['/inventario/ajustes', item.id, 'editar']">
                <mat-icon>edit</mat-icon> Editar borrador
              </a>
            }
            <a mat-button routerLink="/inventario/ajustes"><mat-icon>arrow_back</mat-icon> Volver</a>
          </div>
        </header>

        <div class="summary-grid">
          <article><span>Estado</span><strong class="status" [class]="'status ' + item.estado.toLowerCase()">{{ item.estado }}</strong></article>
          <article><span>Fecha</span><strong>{{ item.fechaAjuste | date:'dd/MM/yyyy HH:mm' }}</strong></article>
          <article><span>Impacto total</span><strong>{{ item.impactoCostoTotalSnapshot ?? 0 | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
          <article><span>Detalles</span><strong>{{ item.detalles.length }}</strong></article>
        </div>

        <section class="card">
          <h2>Motivo</h2>
          <p>{{ item.motivo }}</p>
          @if (item.observaciones) {
            <h3>Observaciones</h3>
            <p>{{ item.observaciones }}</p>
          }
        </section>

        @if (item.fechaConfirmacion || item.fechaAnulacion) {
          <section class="card audit">
            <h2>Trazabilidad</h2>
            @if (item.fechaConfirmacion) {
              <p><strong>Confirmado:</strong> {{ item.fechaConfirmacion | date:'dd/MM/yyyy HH:mm' }} · {{ item.confirmadoPorNombreUsuario || 'Usuario no disponible' }}</p>
            }
            @if (item.fechaAnulacion) {
              <p><strong>Anulado:</strong> {{ item.fechaAnulacion | date:'dd/MM/yyyy HH:mm' }} · {{ item.anuladoPorNombreUsuario || 'Usuario no disponible' }}</p>
              <p><strong>Motivo de anulación:</strong> {{ item.motivoAnulacion || 'Sin motivo registrado' }}</p>
            }
          </section>
        }

        <section class="card">
          <h2>Detalle de productos</h2>
          <div class="table-shell">
            <table>
              <thead>
                <tr>
                  <th>Producto</th>
                  <th>SKU</th>
                  <th>Variante</th>
                  <th>Anterior</th>
                  <th>Objetivo</th>
                  <th>Diferencia</th>
                  <th>Costo unit.</th>
                  <th>Impacto</th>
                </tr>
              </thead>
              <tbody>
                @for (detalle of item.detalles; track detalle.id) {
                  <tr>
                    <td>{{ detalle.nombreSnapshot || ('Producto #' + detalle.productoId) }}</td>
                    <td>{{ detalle.skuSnapshot || '—' }}</td>
                    <td>{{ variante(detalle) }}</td>
                    <td>{{ detalle.cantidadAnteriorSnapshot ?? '—' }}</td>
                    <td>{{ detalle.cantidadNuevaSnapshot ?? detalle.cantidadObjetivo }}</td>
                    <td>{{ detalle.diferenciaSnapshot ?? '—' }}</td>
                    <td>{{ detalle.costoUnitarioSnapshot == null ? '—' : (detalle.costoUnitarioSnapshot | currency:'HNL':'symbol-narrow':'1.2-2') }}</td>
                    <td>{{ detalle.impactoCostoSnapshot == null ? '—' : (detalle.impactoCostoSnapshot | currency:'HNL':'symbol-narrow':'1.2-2') }}</td>
                  </tr>
                } @empty {
                  <tr><td colspan="8" class="empty">No hay detalles registrados.</td></tr>
                }
              </tbody>
            </table>
          </div>
        </section>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display: block; }
    .detail-page { padding: 24px; max-width: 1500px; margin: 0 auto; }
    .page-header { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 24px; }
    .eyebrow { margin: 0 0 4px; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .08em; opacity: .65; }
    h1 { margin: 0; font-size: clamp(24px, 3vw, 34px); }
    h2 { margin: 0 0 16px; font-size: 19px; }
    h3 { margin: 18px 0 8px; font-size: 15px; }
    .subtitle, .card p { margin: 6px 0 0; opacity: .76; }
    .header-actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .summary-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; margin-bottom: 18px; }
    .summary-grid article, .card { border: 1px solid rgba(127,127,127,.22); border-radius: 12px; padding: 18px; }
    .summary-grid article { display: grid; gap: 8px; }
    .summary-grid span { font-size: 12px; text-transform: uppercase; letter-spacing: .04em; opacity: .65; }
    .card { margin-bottom: 18px; }
    .status { display: inline-flex; width: fit-content; padding: 4px 9px; border-radius: 999px; background: rgba(127,127,127,.14); }
    .status.confirmado { background: rgba(46,125,50,.14); }
    .status.anulado { background: rgba(198,40,40,.14); }
    .status.borrador { background: rgba(245,124,0,.14); }
    .table-shell { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; min-width: 980px; }
    th, td { padding: 12px 14px; text-align: left; border-bottom: 1px solid rgba(127,127,127,.16); }
    th { font-size: 12px; text-transform: uppercase; letter-spacing: .04em; opacity: .7; }
    .empty { text-align: center; opacity: .65; }
    .loading, .error { display: flex; align-items: center; justify-content: center; gap: 10px; min-height: 220px; }
    .error { justify-content: flex-start; min-height: auto; border: 1px solid rgba(244,67,54,.32); border-radius: 12px; padding: 18px; background: rgba(244,67,54,.06); }
    @media (max-width: 900px) { .summary-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
    @media (max-width: 640px) { .detail-page { padding: 16px; } .page-header { flex-direction: column; } .summary-grid { grid-template-columns: 1fr; } }
  `]
})
export class AjusteDetailComponent implements OnInit {
  readonly ajuste = signal<AjusteInventario | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly puedeEditar = signal(false);

  private readonly ajusteId: number;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly ajusteService: AjusteInventarioService,
    private readonly permisosRuntime: PermisosRuntimeService
  ) {
    this.ajusteId = Number(this.route.snapshot.paramMap.get('id'));
  }

  ngOnInit(): void {
    this.puedeEditar.set(this.permisosRuntime.puede('Inventario', 'Editar'));
    if (!Number.isInteger(this.ajusteId) || this.ajusteId <= 0) {
      this.loading.set(false);
      this.error.set('El identificador del ajuste no es válido.');
      return;
    }

    this.ajusteService.getById(this.ajusteId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.success || !response.data) {
            this.error.set(response.message || 'No fue posible cargar el ajuste.');
            return;
          }
          this.ajuste.set(response.data);
        },
        error: (err) => this.error.set(
          err?.error?.message || err?.error?.title || err?.message || 'No fue posible cargar el ajuste.'
        )
      });
  }

  variante(detalle: AjusteInventario['detalles'][number]): string {
    return [detalle.marcaSnapshot, detalle.modeloSnapshot, detalle.colorSnapshot, detalle.tallaSnapshot]
      .filter(Boolean)
      .join(' · ') || '—';
  }
}
