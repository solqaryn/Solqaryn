
import { Component, Input, OnInit, forwardRef, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MetodoPago } from '../../core/models/metodo-pago.model';
import { MetodoPagoService } from '../../services/metodo-pago.service';

@Component({
  selector: 'app-metodo-pago-select',
  standalone: true,
  imports: [MatFormFieldModule, MatSelectModule, MatProgressSpinnerModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MetodoPagoSelectComponent),
      multi: true
    }
  ],
  template: `
    <mat-form-field appearance="outline" class="full-width">
      <mat-label>{{ label }}</mat-label>
      <mat-select
        [value]="value()"
        [disabled]="disabled || loading()"
        (selectionChange)="seleccionar($event.value)"
        (closed)="onTouched()">
        @for (metodo of metodos(); track metodo.id) {
          <mat-option [value]="metodo.codigo">{{ metodo.nombre }}</mat-option>
        }
        @if (esValorHistorico()) {
          <mat-option [value]="value()" disabled>{{ value() }} (histórico/inactivo)</mat-option>
        }
      </mat-select>
      @if (loading()) {
        <mat-spinner matSuffix diameter="18"></mat-spinner>
      }
      @if (error()) {
        <mat-hint>{{ error() }}</mat-hint>
      } @else if (esValorHistorico()) {
        <mat-hint>El método histórico se conserva solo para lectura; selecciona uno activo para una nueva operación.</mat-hint>
      }
    </mat-form-field>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [':host { display: block; min-width: 0; } .full-width { width: 100%; }']
})
export class MetodoPagoSelectComponent implements OnInit, ControlValueAccessor {
  private readonly metodoPagoService = inject(MetodoPagoService);

  @Input() label = 'Método de pago';
  @Input() preservarValorActual = false;

  readonly metodos = signal<MetodoPago[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly value = signal<string | null>(null);
  disabled = false;

  private onChange: (value: string | null) => void = () => undefined;
  onTouched: () => void = () => undefined;

  ngOnInit(): void {
    this.metodoPagoService.getActivos().subscribe({
      next: (res) => {
        const activos = [...res.data].filter((metodo) => metodo.activo)
          .sort((a, b) => a.orden - b.orden || a.codigo.localeCompare(b.codigo));
        this.metodos.set(activos);
        this.loading.set(false);
        this.normalizarValor();
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudieron cargar los métodos de pago activos.');
        if (!this.preservarValorActual) this.establecerValor(null, true);
      }
    });
  }

  writeValue(value: string | null | undefined): void {
    this.value.set(value?.trim() || null);
    if (!this.loading()) this.normalizarValor();
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  seleccionar(value: string): void {
    this.establecerValor(value, true);
  }

  esValorHistorico(): boolean {
    const actual = this.value();
    return !!actual && !this.buscarActivo(actual);
  }

  private normalizarValor(): void {
    const activos = this.metodos();
    if (activos.length === 0) {
      this.error.set('No hay métodos de pago activos disponibles.');
      if (!this.preservarValorActual) this.establecerValor(null, true);
      return;
    }

    const actual = this.value();
    const coincidencia = actual ? this.buscarActivo(actual) : undefined;
    if (coincidencia) {
      if (coincidencia.codigo !== actual) this.establecerValor(coincidencia.codigo, true);
      return;
    }

    if (!actual || !this.preservarValorActual) this.establecerValor(activos[0].codigo, true);
  }

  private buscarActivo(value: string): MetodoPago | undefined {
    const normalizado = value.trim().toLocaleLowerCase('es');
    return this.metodos().find((metodo) =>
      metodo.codigo.toLocaleLowerCase('es') === normalizado
      || metodo.nombre.toLocaleLowerCase('es') === normalizado
    );
  }

  private establecerValor(value: string | null, emitir: boolean): void {
    this.value.set(value);
    if (emitir) this.onChange(value);
  }
}
