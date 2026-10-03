
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RentabilidadAgrupacion } from '../../../core/models/reporte-rentabilidad.models';

export type { RentabilidadAgrupacion } from '../../../core/models/reporte-rentabilidad.models';

@Component({
  selector: 'app-rentabilidad-agrupacion',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="agrupacion-container">
      <label for="agrupacion-select" class="agrupacion-label">Agrupar por</label>
      <select
        id="agrupacion-select"
        class="agrupacion-select"
        [formControl]="agrupacionControl"
        (change)="onSelectionChange()"
        aria-label="Seleccionar agrupación de rentabilidad"
      >
        <option value="vendedor">Vendedor</option>
        <option value="cliente">Cliente</option>
        <option value="producto">Producto</option>
        <option value="categoria">Categoría</option>
      </select>
    </div>
  `,
  styles: [`
    .agrupacion-container { display: flex; flex-direction: column; gap: 0.5rem; }
    .agrupacion-label { font-weight: 500; font-size: 0.875rem; color: #374151; }
    .agrupacion-select {
      padding: 0.5rem;
      border: 1px solid #d1d5db;
      border-radius: 0.375rem;
      background-color: #fff;
      font-size: 0.875rem;
      color: #1f2937;
      min-width: 200px;
    }
    .agrupacion-select:focus { outline: 2px solid #3b82f6; outline-offset: 2px; }
    .agrupacion-select:disabled { background-color: #f3f4f6; cursor: not-allowed; opacity: 0.7; }
  `],
})
export class RentabilidadAgrupacionComponent {
  @Input() set value(value: RentabilidadAgrupacion) {
    if (this.agrupacionControl.value !== value) {
      this.agrupacionControl.setValue(value, { emitEvent: false });
    }
  }

  @Input() set disabled(disabled: boolean) {
    if (disabled) {
      this.agrupacionControl.disable({ emitEvent: false });
    } else {
      this.agrupacionControl.enable({ emitEvent: false });
    }
  }

  @Output() readonly valueChange = new EventEmitter<RentabilidadAgrupacion>();

  readonly agrupacionControl = new FormControl<RentabilidadAgrupacion>('vendedor', { nonNullable: true });

  onSelectionChange(): void {
    if (this.agrupacionControl.disabled) {
      return;
    }
    this.valueChange.emit(this.agrupacionControl.value);
  }
}
