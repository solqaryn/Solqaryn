
import { Component, Inject, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { EstadoRecepcionCompra, RecepcionCompra } from '../../core/models/recepcion-compra.model';
import { RecepcionCompraService } from '../../services/recepcion-compra.service';
import { AnularDialogComponent } from '../../shared/anular-dialog.component';

@Component({
  selector: 'app-confirmar-recepcion-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>{{ data.message }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [mat-dialog-close]="false">Cancelar</button>
      <button mat-flat-button type="button" [mat-dialog-close]="true">Confirmar</button>
    </mat-dialog-actions>
  `
})
export class ConfirmarRecepcionDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { title: string; message: string }) {}
}

@Component({
  selector: 'app-recepcion-compra-detail',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <section class="page-shell" aria-labelledby="recepcion-detail-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Compras empresariales</p>
          <h1 id="recepcion-detail-title">Detalle de recepción</h1>
          @if (recepcion()) { <p><strong>{{ recepcion()!.numeroRecepcion }}</strong> · Orden {{ recepcion()!.numeroOrdenCompra || ('#' + recepcion()!.ordenCompraId) }}</p> }
        </div>
        <button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon> Volver</button>
      </header>

      @if (!puedeVer()) {
        <div class="state-panel error" role="alert"><mat-icon>lock</mat-icon><span>No tienes permiso para consultar esta recepción de compra.</span></div>
      } @else if (loading()) {
        <div class="state-panel" role="status"><mat-spinner diameter="36"></mat-spinner><span>Cargando recepción…</span></div>
      } @else if (error()) {
        <div class="state-panel error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error() }}</span><button mat-stroked-button type="button" (click)="cargar()">Reintentar</button></div>
      } @else if (recepcion(); as item) {
        <div class="summary-grid">
          <div><small>Estado</small><strong class="status" [attr.data-status]="item.estado">{{ estadoNombre(item.estado) }}</strong></div>
          <div><small>Recibida</small><strong>{{ item.cantidadRecibidaTotal }}</strong></div>
          <div><small>Aceptada</small><strong>{{ item.cantidadAceptadaTotal }}</strong></div>
          <div><small>Dañada</small><strong>{{ item.cantidadDanadaTotal }}</strong></div>
          <div><small>Faltante</small><strong>{{ item.cantidadFaltanteTotal }}</strong></div>
          <div><small>Sobrante</small><strong>{{ item.cantidadSobranteTotal }}</strong></div>
        </div>

        @if (item.observaciones) { <div class="note"><strong>Observaciones</strong><p>{{ item.observaciones }}</p></div> }

        <div class="table-wrap">
          <table>
            <thead><tr><th>Producto</th><th>Almacén</th><th class="numeric">Recibida</th><th class="numeric">Aceptada</th><th class="numeric">Dañada</th><th class="numeric">Faltante</th><th class="numeric">Sobrante</th></tr></thead>
            <tbody>
              @for (detalle of item.detalles; track detalle.id) {
                <tr>
                  <td><strong>{{ detalle.productoSkuSnapshot || ('#' + detalle.productoId) }}</strong><span>{{ detalle.productoNombreSnapshot || '' }}</span></td>
                  <td>#{{ detalle.almacenId }}@if (detalle.ubicacionAlmacenId) { · Ubicación #{{ detalle.ubicacionAlmacenId }} }</td>
                  <td class="numeric">{{ detalle.cantidadRecibida }}</td>
                  <td class="numeric">{{ detalle.cantidadAceptada }}</td>
                  <td class="numeric">{{ detalle.cantidadDanada }}</td>
                  <td class="numeric">{{ detalle.cantidadFaltante }}</td>
                  <td class="numeric">{{ detalle.cantidadSobrante }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="actions">
          @if (esBorrador(item.estado) && puedeConfirmar()) {
            <button mat-flat-button type="button" [disabled]="procesando()" (click)="confirmar()" data-testid="confirmar-recepcion"><mat-icon>check_circle</mat-icon> Confirmar recepción</button>
          }
          @if (esRecibida(item.estado) && puedeAnular()) {
            <button mat-stroked-button type="button" [disabled]="procesando()" (click)="anular()" data-testid="anular-recepcion"><mat-icon>undo</mat-icon> Anular recepción</button>
          }
          @if (procesando()) { <mat-spinner diameter="24" aria-label="Procesando acción de recepción"></mat-spinner> }
        </div>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page-shell{display:grid;gap:1.25rem;max-width:1400px;margin:0 auto}.page-header{display:flex;justify-content:space-between;align-items:flex-start;gap:1rem}.eyebrow{margin:0 0 .25rem;text-transform:uppercase;letter-spacing:.08em;font-size:.75rem;font-weight:700;opacity:.7}h1{margin:.1rem 0}.state-panel{min-height:120px;display:flex;align-items:center;justify-content:center;gap:.75rem;border:1px solid rgba(127,127,127,.18);border-radius:14px}.error{color:var(--mat-sys-error,#b3261e)}.summary-grid{display:grid;grid-template-columns:repeat(6,minmax(120px,1fr));gap:.75rem}.summary-grid>div{display:grid;gap:.3rem;padding:1rem;border:1px solid rgba(127,127,127,.18);border-radius:12px}.summary-grid small{opacity:.7}.summary-grid strong{font-size:1.1rem}.note{padding:1rem;border-radius:12px;background:rgba(127,127,127,.08)}.note p{margin:.4rem 0 0}.table-wrap{overflow:auto;border:1px solid rgba(127,127,127,.18);border-radius:12px}table{width:100%;border-collapse:collapse;min-width:900px}th,td{padding:.75rem;border-bottom:1px solid rgba(127,127,127,.15);text-align:left}td:first-child{display:grid;gap:.2rem}td:first-child span{opacity:.72}.numeric{text-align:right}.actions{display:flex;justify-content:flex-end;align-items:center;gap:.75rem}@media(max-width:900px){.summary-grid{grid-template-columns:repeat(3,1fr)}}@media(max-width:620px){.page-header{flex-direction:column}.summary-grid{grid-template-columns:repeat(2,1fr)}}
  `]
})
export class RecepcionCompraDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(RecepcionCompraService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly dialog = inject(MatDialog);

  readonly recepcion = signal<RecepcionCompra | null>(null);
  readonly loading = signal(false);
  readonly procesando = signal(false);
  readonly error = signal('');
  readonly puedeVer = signal(false);
  readonly puedeConfirmar = signal(false);
  readonly puedeAnular = signal(false);
  private recepcionId = 0;

  ngOnInit(): void {
    this.puedeVer.set(this.permisos.puede('Compras', 'Ver'));
    this.puedeConfirmar.set(this.permisos.puede('Compras', 'Confirmar'));
    this.puedeAnular.set(this.permisos.puede('Compras', 'Anular'));
    if (!this.puedeVer()) return;

    this.recepcionId = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(this.recepcionId) || this.recepcionId <= 0) {
      this.error.set('El identificador de la recepción no es válido.');
      return;
    }
    this.cargar();
  }

  cargar(): void {
    if (!this.puedeVer() || this.recepcionId <= 0 || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.service.getById(this.recepcionId).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => this.recepcion.set(response.data),
      error: () => this.error.set('No fue posible cargar la recepción de compra.')
    });
  }

  confirmar(): void {
    const actual = this.recepcion();
    if (!actual || !this.esBorrador(actual.estado) || !this.puedeConfirmar() || this.procesando()) return;
    this.procesando.set(true);
    const ref = this.dialog.open(ConfirmarRecepcionDialogComponent, {
      disableClose: true,
      data: {
        title: 'Confirmar recepción',
        message: 'Al confirmar se materializará la cantidad aceptada en el stock físico. Esta acción queda auditada.'
      }
    });
    ref.afterClosed().subscribe((confirmado: boolean | undefined) => {
      if (!confirmado) {
        this.procesando.set(false);
        return;
      }
      this.error.set('');
      this.service.confirmar(this.recepcionId).pipe(finalize(() => this.procesando.set(false))).subscribe({
        next: response => this.recepcion.set(response.data),
        error: () => this.error.set('No fue posible confirmar la recepción.')
      });
    });
  }

  anular(): void {
    const actual = this.recepcion();
    if (!actual || !this.esRecibida(actual.estado) || !this.puedeAnular() || this.procesando()) return;
    this.procesando.set(true);
    const ref = this.dialog.open(AnularDialogComponent, {
      disableClose: true,
      data: {
        title: 'Anular recepción de compra',
        message: 'La anulación revertirá el stock materializado. Indica el motivo obligatorio:'
      }
    });
    ref.afterClosed().subscribe((motivo: string | undefined) => {
      const normalizado = motivo?.trim();
      if (!normalizado) {
        this.procesando.set(false);
        return;
      }
      this.error.set('');
      this.service.anular(this.recepcionId, normalizado).pipe(finalize(() => this.procesando.set(false))).subscribe({
        next: response => this.recepcion.set(response.data),
        error: () => this.error.set('No fue posible anular la recepción.')
      });
    });
  }

  volver(): void { void this.router.navigate(['/recepciones-compra']); }

  estadoNombre(estado: EstadoRecepcionCompra): string {
    return ({ '1': 'Borrador', '2': 'Recibida', '3': 'Anulada', Borrador: 'Borrador', Recibida: 'Recibida', Anulada: 'Anulada' } as Record<string,string>)[String(estado)] ?? String(estado);
  }
  esBorrador(estado: EstadoRecepcionCompra): boolean { return estado === 1 || estado === 'Borrador'; }
  esRecibida(estado: EstadoRecepcionCompra): boolean { return estado === 2 || estado === 'Recibida'; }
}
