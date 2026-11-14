
import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ReporteComprasFiltroDto } from '../../../core/models/reporte-compras.models';

export interface ReporteComprasFiltroOpcion {
  id: number;
  nombre: string;
}

export interface ReporteComprasFiltroOpciones {
  proveedores?: ReporteComprasFiltroOpcion[];
  productos?: ReporteComprasFiltroOpcion[];
  variantes?: ReporteComprasFiltroOpcion[];
}

type ReporteComprasSelectorControl = 'proveedorId' | 'productoId' | 'productoVarianteId';

@Component({
  selector: 'app-reporte-compras-filtros',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form class="filter-grid" [formGroup]="form" (ngSubmit)="emitir()" aria-label="Filtros del reporte de compras">
      <label>Desde<input type="date" formControlName="desdeUtc" /></label>
      <label>Hasta<input type="date" formControlName="hastaUtc" /></label>

      @for (selector of selectores; track selector.control) {
        <label>
          {{ selector.label }}
          <select [formControlName]="selector.control" [attr.aria-busy]="cargandoSelectores">
            <option [ngValue]="null">Todos</option>
            @for (opcion of selector.options; track opcion.id) {
              <option [ngValue]="opcion.id">{{ opcion.nombre }}</option>
            }
          </select>
        </label>
      }

      <label>
        Tamaño de página
        <select formControlName="pageSize">
          <option [ngValue]="20">20</option>
          <option [ngValue]="50">50</option>
          <option [ngValue]="100">100</option>
        </select>
      </label>

      <button type="submit" [disabled]="disabled || form.invalid || rangoInvalido || cargandoSelectores">Aplicar filtros</button>

      @if (cargandoSelectores) {
        <p class="status" role="status">Cargando opciones de filtros…</p>
      }
      @if (errorSelectores) {
        <p class="error" role="alert">{{ errorSelectores }}</p>
      }
      @if (rangoInvalido) {
        <p class="error" role="alert">Desde no puede ser posterior a Hasta.</p>
      }
    </form>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [
    `.filter-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:.75rem;align-items:end}`,
    `.filter-grid label{display:grid;gap:.25rem}`,
    `.filter-grid input,.filter-grid select,.filter-grid button{min-height:2.5rem;max-width:100%}`,
    `.status,.error{grid-column:1/-1;margin:0}`,
    `.error{color:#b91c1c}`,
  ],
})
export class ReporteComprasFiltrosComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  @Input() opciones: ReporteComprasFiltroOpciones = {};
  @Input() cargandoSelectores = false;
  @Input() disabled = false;
  @Input() errorSelectores = '';
  @Output() readonly filterChanged = new EventEmitter<ReporteComprasFiltroDto>();

  readonly form = this.fb.group({
    desdeUtc: [''],
    hastaUtc: [''],
    proveedorId: [null as number | null, [Validators.min(1)]],
    productoId: [null as number | null, [Validators.min(1)]],
    productoVarianteId: [null as number | null, [Validators.min(1)]],
    pageSize: [50, [Validators.required, Validators.min(1), Validators.max(100)]],
  });

  ngOnChanges(_changes: SimpleChanges): void {
    if (this.disabled || this.cargandoSelectores) {
      this.form.disable({ emitEvent: false });
      return;
    }

    this.form.enable({ emitEvent: false });
  }

  get selectores(): Array<{ label: string; control: ReporteComprasSelectorControl; options: ReporteComprasFiltroOpcion[] }> {
    return [
      { label: 'Proveedor', control: 'proveedorId', options: this.opciones.proveedores ?? [] },
      { label: 'Producto', control: 'productoId', options: this.opciones.productos ?? [] },
      { label: 'Variante', control: 'productoVarianteId', options: this.opciones.variantes ?? [] },
    ];
  }

  get rangoInvalido(): boolean {
    const { desdeUtc, hastaUtc } = this.form.getRawValue();
    return !!desdeUtc && !!hastaUtc && desdeUtc > hastaUtc;
  }

  emitir(): void {
    if (this.disabled || this.form.invalid || this.rangoInvalido || this.cargandoSelectores) return;
    const value = this.form.getRawValue();
    this.filterChanged.emit({
      page: 1,
      pageSize: value.pageSize ?? 50,
      ...(value.desdeUtc ? { desdeUtc: `${value.desdeUtc}T00:00:00.000Z` } : {}),
      ...(value.hastaUtc ? { hastaUtc: `${value.hastaUtc}T23:59:59.999Z` } : {}),
      ...(value.proveedorId ? { proveedorId: value.proveedorId } : {}),
      ...(value.productoId ? { productoId: value.productoId } : {}),
      ...(value.productoVarianteId ? { productoVarianteId: value.productoVarianteId } : {}),
    });
  }
}
