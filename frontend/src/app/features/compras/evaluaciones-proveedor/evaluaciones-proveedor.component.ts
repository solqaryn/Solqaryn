import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { PermisosRuntimeService } from '../../../core/auth/permisos-runtime.service';
import { EvaluacionProveedor, EvaluacionProveedorFiltro } from '../../../core/models/evaluacion-proveedor.model';
import { EvaluacionProveedorService } from '../../../services/evaluacion-proveedor.service';

@Component({
  selector: 'app-evaluaciones-proveedor',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  template: `
    <section class="page" aria-labelledby="evaluaciones-title">
      <header>
        <div>
          <h1 id="evaluaciones-title">Evaluación de proveedores</h1>
          <p>Consulta evidencia objetiva de entrega y recepción. Este módulo no calcula scoring ni ranking.</p>
        </div>
      </header>

      <mat-card>
        <mat-card-content>
          <form class="filters" [formGroup]="filtros" (ngSubmit)="aplicarFiltros()">
            <mat-form-field appearance="outline">
              <mat-label>Proveedor ID</mat-label>
              <input matInput type="number" min="1" formControlName="proveedorId" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Orden de compra ID</mat-label>
              <input matInput type="number" min="1" formControlName="ordenCompraId" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Recepción ID</mat-label>
              <input matInput type="number" min="1" formControlName="recepcionCompraId" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Desde</mat-label>
              <input matInput type="date" formControlName="desde" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Hasta</mat-label>
              <input matInput type="date" formControlName="hasta" />
            </mat-form-field>
            <div class="filter-actions">
              <button mat-flat-button color="primary" type="submit">Aplicar</button>
              <button mat-button type="button" (click)="limpiarFiltros()">Limpiar</button>
            </div>
          </form>
        </mat-card-content>
      </mat-card>

      @if (puedeCrear()) {
        <mat-card class="generate-card">
          <mat-card-content>
            <form class="generate" [formGroup]="generarForm" (ngSubmit)="generar()">
              <div>
                <strong>Generar evaluación desde recepción</strong>
                <p>Solo crea/actualiza la evaluación a partir de una recepción válida y ya materializada.</p>
              </div>
              <mat-form-field appearance="outline">
                <mat-label>Recepción ID</mat-label>
                <input matInput type="number" min="1" formControlName="recepcionCompraId" aria-label="Recepción de compra para generar evaluación" />
              </mat-form-field>
              <button mat-flat-button color="primary" type="submit" [disabled]="generando()">
                @if (generando()) { Generando... } @else { Generar }
              </button>
            </form>
          </mat-card-content>
        </mat-card>
      }

      <mat-card>
        <mat-card-content>
          @if (loading()) {
            <div class="center" role="status" aria-live="polite"><mat-spinner diameter="42"></mat-spinner><span>Cargando evaluaciones...</span></div>
          } @else if (error()) {
            <div class="center error" role="alert">No fue posible cargar las evaluaciones. <button mat-button (click)="cargar()">Reintentar</button></div>
          } @else {
            <div class="table" tabindex="0" aria-label="Resultados de evaluación de proveedores">
              <table>
                <thead>
                  <tr>
                    <th>ID</th><th>Proveedor</th><th>Orden</th><th>Recepción</th><th>Esperada</th><th>Recibida</th><th>Aceptada</th><th>Dañada</th><th>Sobrante</th><th></th>
                  </tr>
                </thead>
                <tbody>
                  @for (e of evaluaciones(); track e.id) {
                    <tr [class.selected]="seleccionada()?.id === e.id">
                      <td>{{ e.id }}</td>
                      <td>#{{ e.proveedorId }}</td>
                      <td>#{{ e.ordenCompraId }}</td>
                      <td>#{{ e.recepcionCompraId }}</td>
                      <td>{{ e.fechaEsperadaUtc | date:'dd/MM/yyyy HH:mm':'UTC' }}</td>
                      <td>{{ e.fechaRecepcionUtc | date:'dd/MM/yyyy HH:mm':'UTC' }}</td>
                      <td>{{ e.cantidadAceptada | number:'1.0-4' }} / {{ e.cantidadOrdenada | number:'1.0-4' }}</td>
                      <td>{{ e.cantidadDanada | number:'1.0-4' }}</td>
                      <td>{{ e.cantidadSobrante | number:'1.0-4' }}</td>
                      <td><button mat-icon-button type="button" (click)="verDetalle(e.id)" [attr.aria-label]="'Ver evaluación ' + e.id"><mat-icon>visibility</mat-icon></button></td>
                    </tr>
                  } @empty {
                    <tr><td colspan="10" class="empty">No hay evaluaciones para los filtros seleccionados.</td></tr>
                  }
                </tbody>
              </table>
            </div>
            <footer>
              <span>{{ totalCount() }} registro(s)</span>
              <div class="pager">
                <button mat-button type="button" [disabled]="page <= 1" (click)="cambiarPagina(page - 1)">Anterior</button>
                <span>Página {{ page }} de {{ totalPages() || 1 }}</span>
                <button mat-button type="button" [disabled]="page >= totalPages()" (click)="cambiarPagina(page + 1)">Siguiente</button>
              </div>
            </footer>
          }
        </mat-card-content>
      </mat-card>

      @if (detalleLoading()) {
        <div class="center" role="status"><mat-spinner diameter="36"></mat-spinner></div>
      } @else if (seleccionada(); as d) {
        <mat-card class="detail" aria-labelledby="detalle-title">
          <mat-card-header><mat-card-title id="detalle-title">Evaluación #{{ d.id }}</mat-card-title></mat-card-header>
          <mat-card-content>
            <dl>
              <div><dt>Proveedor</dt><dd>#{{ d.proveedorId }}</dd></div>
              <div><dt>Orden de compra</dt><dd>#{{ d.ordenCompraId }}</dd></div>
              <div><dt>Recepción</dt><dd>#{{ d.recepcionCompraId }}</dd></div>
              <div><dt>Fecha esperada UTC</dt><dd>{{ d.fechaEsperadaUtc | date:'medium':'UTC' }}</dd></div>
              <div><dt>Fecha recepción UTC</dt><dd>{{ d.fechaRecepcionUtc | date:'medium':'UTC' }}</dd></div>
              <div><dt>Cantidad ordenada</dt><dd>{{ d.cantidadOrdenada | number:'1.0-4' }}</dd></div>
              <div><dt>Cantidad aceptada</dt><dd>{{ d.cantidadAceptada | number:'1.0-4' }}</dd></div>
              <div><dt>Cantidad dañada</dt><dd>{{ d.cantidadDanada | number:'1.0-4' }}</dd></div>
              <div><dt>Cantidad sobrante</dt><dd>{{ d.cantidadSobrante | number:'1.0-4' }}</dd></div>
            </dl>
          </mat-card-content>
        </mat-card>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page{max-width:1280px;margin:0 auto;padding:16px;display:grid;gap:16px}header h1{margin-bottom:4px}header p,.generate p{margin:0;color:var(--color-text-secondary,#666)}
    .filters{display:grid;grid-template-columns:repeat(5,minmax(150px,1fr));gap:12px;align-items:start}.filter-actions{display:flex;gap:8px;align-items:center;grid-column:1/-1}
    .generate{display:grid;grid-template-columns:1fr minmax(180px,280px) auto;gap:16px;align-items:center}.table{overflow:auto;max-width:100%}table{width:100%;border-collapse:collapse;min-width:1050px}th,td{padding:10px 12px;text-align:left;border-bottom:1px solid var(--color-border,#ddd);white-space:nowrap}.selected{background:rgba(25,118,210,.08)}.empty{text-align:center;padding:28px}
    footer{display:flex;justify-content:space-between;align-items:center;gap:12px;padding-top:12px}.pager{display:flex;align-items:center;gap:8px}.center{display:flex;gap:12px;align-items:center;justify-content:center;padding:32px}.error{color:var(--color-danger,#b00020)}
    dl{display:grid;grid-template-columns:repeat(3,minmax(160px,1fr));gap:12px}dl div{padding:10px;border:1px solid var(--color-border,#ddd);border-radius:6px}dt{font-size:.85rem;color:var(--color-text-secondary,#666)}dd{margin:4px 0 0;font-weight:600}
    @media(max-width:900px){.filters{grid-template-columns:1fr 1fr}.generate{grid-template-columns:1fr}.generate button{justify-self:start}dl{grid-template-columns:1fr 1fr}}@media(max-width:600px){.filters,dl{grid-template-columns:1fr}footer{align-items:flex-start;flex-direction:column}}
  `]
})
export class EvaluacionesProveedorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(EvaluacionProveedorService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly snack = inject(MatSnackBar);

  readonly evaluaciones = signal<EvaluacionProveedor[]>([]);
  readonly seleccionada = signal<EvaluacionProveedor | null>(null);
  readonly loading = signal(false);
  readonly detalleLoading = signal(false);
  readonly generando = signal(false);
  readonly error = signal(false);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly puedeCrear = signal(false);

  page = 1;
  readonly pageSize = 20;

  readonly filtros = this.fb.group({
    proveedorId: [null as number | null],
    ordenCompraId: [null as number | null],
    recepcionCompraId: [null as number | null],
    desde: [''],
    hasta: ['']
  });

  readonly generarForm = this.fb.group({ recepcionCompraId: [null as number | null] });

  ngOnInit(): void {
    this.puedeCrear.set(this.permisos.puede('Compras', 'Crear'));
    this.cargar();
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.filtros.reset({ proveedorId: null, ordenCompraId: null, recepcionCompraId: null, desde: '', hasta: '' });
    this.page = 1;
    this.cargar();
  }

  cambiarPagina(page: number): void {
    if (page < 1 || (this.totalPages() > 0 && page > this.totalPages())) return;
    this.page = page;
    this.cargar();
  }

  cargar(): void {
    const raw = this.filtros.getRawValue();
    const filtro: EvaluacionProveedorFiltro = {
      page: this.page,
      pageSize: this.pageSize,
      proveedorId: this.idValido(raw.proveedorId),
      ordenCompraId: this.idValido(raw.ordenCompraId),
      recepcionCompraId: this.idValido(raw.recepcionCompraId),
      desdeUtc: this.inicioUtc(raw.desde),
      hastaUtc: this.finUtc(raw.hasta)
    };

    this.loading.set(true);
    this.error.set(false);
    this.service.getPaged(filtro).subscribe({
      next: response => {
        this.evaluaciones.set(response.data.items);
        this.totalCount.set(response.data.totalCount);
        this.totalPages.set(response.data.totalPages);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      }
    });
  }

  verDetalle(id: number): void {
    this.detalleLoading.set(true);
    this.service.getById(id).subscribe({
      next: response => {
        this.seleccionada.set(response.data);
        this.detalleLoading.set(false);
      },
      error: () => {
        this.detalleLoading.set(false);
        this.snack.open('No fue posible cargar el detalle de la evaluación.', 'Cerrar', { duration: 4500 });
      }
    });
  }

  generar(): void {
    if (!this.puedeCrear()) {
      this.snack.open('No tiene permiso Compras/Crear.', 'Cerrar', { duration: 4000 });
      return;
    }
    const recepcionId = this.idValido(this.generarForm.controls.recepcionCompraId.value);
    if (!recepcionId) {
      this.snack.open('Ingrese una recepción válida.', 'Cerrar', { duration: 3500 });
      return;
    }

    this.generando.set(true);
    this.service.generarPorRecepcion(recepcionId).subscribe({
      next: response => {
        this.generando.set(false);
        this.seleccionada.set(response.data);
        this.generarForm.reset({ recepcionCompraId: null });
        this.snack.open('Evaluación de proveedor generada correctamente.', 'Cerrar', { duration: 3500 });
        this.page = 1;
        this.cargar();
      },
      error: err => {
        this.generando.set(false);
        this.snack.open(err.error?.detail || err.error?.message || 'No fue posible generar la evaluación.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  private idValido(value: number | null | undefined): number | null {
    const n = Number(value);
    return Number.isInteger(n) && n > 0 ? n : null;
  }

  private inicioUtc(value: string | null | undefined): string | null {
    return value ? new Date(`${value}T00:00:00.000Z`).toISOString() : null;
  }

  private finUtc(value: string | null | undefined): string | null {
    return value ? new Date(`${value}T23:59:59.999Z`).toISOString() : null;
  }
}
