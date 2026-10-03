
import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import { AsientoContableDto } from '../../core/models/asiento-contable.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

interface TipoEventoOption {
  value: number;
  label: string;
}

interface EventoContableDto {
  tipo: number;
  documentoOrigenId: number;
  fecha: string;
  monto: number;
  referencia: string;
  costo?: number;
}

@Component({
  selector: 'app-evento-contable-panel',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
],
  template: `
    @if (puedeContabilizar) {
      <section class="evento-card">
        <div class="evento-header">
          <div>
            <h3>Contabilizar evento</h3>
            <p>Genera el asiento desde la configuración contable vigente, sin seleccionar cuentas manualmente.</p>
          </div>
          <button mat-stroked-button type="button" (click)="toggle()">
            {{ abierto() ? 'Ocultar' : 'Nuevo evento' }}
          </button>
        </div>
        @if (abierto()) {
          <form [formGroup]="form" (ngSubmit)="contabilizar()" class="evento-form">
            <mat-form-field appearance="outline">
              <mat-label>Tipo de evento</mat-label>
              <mat-select formControlName="tipo">
                @for (tipo of tipos; track tipo) {
                  <mat-option [value]="tipo.value">{{ tipo.label }}</mat-option>
                }
              </mat-select>
              @if (form.get('tipo')?.hasError('required')) {
                <mat-error>Selecciona un tipo</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>ID documento origen</mat-label>
              <input matInput type="number" min="1" formControlName="documentoOrigenId">
              @if (form.get('documentoOrigenId')?.invalid) {
                <mat-error>Debe ser mayor que cero</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Fecha</mat-label>
              <input matInput type="datetime-local" formControlName="fecha">
              @if (form.get('fecha')?.hasError('required')) {
                <mat-error>La fecha es obligatoria</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Monto</mat-label>
              <input matInput type="number" min="0.01" step="0.01" formControlName="monto">
              @if (form.get('monto')?.invalid) {
                <mat-error>Debe ser mayor que cero</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Costo (opcional)</mat-label>
              <input matInput type="number" min="0" step="0.01" formControlName="costo">
            </mat-form-field>
            <mat-form-field appearance="outline" class="referencia">
              <mat-label>Referencia</mat-label>
              <input matInput maxlength="200" formControlName="referencia">
              @if (form.get('referencia')?.hasError('required')) {
                <mat-error>La referencia es obligatoria</mat-error>
              }
            </mat-form-field>
            <div class="evento-actions">
              <button mat-button type="button" (click)="toggle()" [disabled]="guardando()">Cancelar</button>
              <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || guardando()">
                @if (guardando()) {
                  <mat-spinner diameter="18"></mat-spinner>
                }
                <span>{{ guardando() ? 'Contabilizando…' : 'Contabilizar' }}</span>
              </button>
            </div>
          </form>
        }
      </section>
    }
    `,
  styles: [`
    .evento-card { margin: 0 0 1.5rem; padding: 1rem; border: 1px solid rgba(0,0,0,.12); border-radius: 8px; }
    .evento-header { display: flex; gap: 1rem; align-items: center; justify-content: space-between; }
    .evento-header h3 { margin: 0; }
    .evento-header p { margin: .25rem 0 0; color: rgba(0,0,0,.65); }
    .evento-form { margin-top: 1rem; display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: .75rem 1rem; }
    .referencia { grid-column: 1 / -1; }
    .evento-actions { grid-column: 1 / -1; display: flex; justify-content: flex-end; gap: .5rem; }
    .evento-actions button mat-spinner { display: inline-block; margin-right: .5rem; }
  `]
})
export class EventoContablePanelComponent {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly snackBar = inject(MatSnackBar);
  private readonly permisos = inject(PermisosRuntimeService);

  @Output() readonly contabilizado = new EventEmitter<AsientoContableDto>();

  readonly abierto = signal(false);
  readonly guardando = signal(false);
  readonly puedeContabilizar = this.permisos.puede('Finanzas', 'Crear');

  readonly tipos: TipoEventoOption[] = [
    { value: 1, label: 'Venta' },
    { value: 2, label: 'Compra' },
    { value: 3, label: 'Cobro' },
    { value: 4, label: 'Pago' },
    { value: 5, label: 'Movimiento de inventario' },
    { value: 6, label: 'Costo de venta' },
    { value: 7, label: 'Devolución de cliente' },
    { value: 8, label: 'Devolución a proveedor' },
    { value: 9, label: 'Ajuste de inventario' },
    { value: 10, label: 'Movimiento de caja' },
    { value: 11, label: 'Movimiento bancario' }
  ];

  readonly form = this.fb.group({
    tipo: [null as number | null, Validators.required],
    documentoOrigenId: [null as number | null, [Validators.required, Validators.min(1)]],
    fecha: [this.localDateTime(), Validators.required],
    monto: [null as number | null, [Validators.required, Validators.min(0.01)]],
    costo: [null as number | null, Validators.min(0)],
    referencia: ['', [Validators.required, Validators.maxLength(200)]]
  });

  toggle(): void {
    if (this.guardando()) return;
    this.abierto.update(value => !value);
  }

  contabilizar(): void {
    if (this.form.invalid || this.guardando() || !this.puedeContabilizar) return;

    const raw = this.form.getRawValue();
    const dto: EventoContableDto = {
      tipo: Number(raw.tipo),
      documentoOrigenId: Number(raw.documentoOrigenId),
      fecha: new Date(raw.fecha ?? '').toISOString(),
      monto: Number(raw.monto),
      referencia: (raw.referencia ?? '').trim()
    };
    if (raw.costo != null) dto.costo = Number(raw.costo);

    this.guardando.set(true);
    this.form.disable();
    this.http.post<ApiResponse<AsientoContableDto>>(`${environment.apiUrl}/contabilizacion/eventos`, dto).subscribe({
      next: response => {
        this.guardando.set(false);
        this.form.enable();
        this.abierto.set(false);
        this.form.reset({ fecha: this.localDateTime(), referencia: '' });
        if (response.data) this.contabilizado.emit(response.data);
        this.snackBar.open('Evento contabilizado correctamente', 'Cerrar', { duration: 3000 });
      },
      error: err => {
        this.guardando.set(false);
        this.form.enable();
        this.snackBar.open(err?.error?.detail || err?.error?.message || 'No fue posible contabilizar el evento', 'Cerrar', { duration: 5000 });
      }
    });
  }

  private localDateTime(): string {
    const date = new Date();
    const offsetMs = date.getTimezoneOffset() * 60_000;
    return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
  }
}
