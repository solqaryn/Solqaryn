
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ReporteVentasFiltroDto } from '../../../core/models/reporte-ventas.models';

export interface RentabilidadFiltroOpcion {
  id: number;
  nombre: string;
}

export interface RentabilidadFiltroOpciones {
  vendedores?: RentabilidadFiltroOpcion[];
  clientes?: RentabilidadFiltroOpcion[];
  sucursales?: RentabilidadFiltroOpcion[];
  categorias?: RentabilidadFiltroOpcion[];
  productos?: RentabilidadFiltroOpcion[];
  variantes?: RentabilidadFiltroOpcion[];
  marcas?: RentabilidadFiltroOpcion[];
  modelos?: RentabilidadFiltroOpcion[];
  colores?: RentabilidadFiltroOpcion[];
  tallas?: RentabilidadFiltroOpcion[];
}

type RentabilidadSelectorControl =
  | 'vendedorId'
  | 'clienteId'
  | 'sucursalId'
  | 'categoriaId'
  | 'productoId'
  | 'varianteId'
  | 'marcaId'
  | 'modeloId'
  | 'colorId'
  | 'tallaId';

@Component({
  selector: 'app-rentabilidad-filtros',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form class="filter-grid" [formGroup]="form" (ngSubmit)="emitir()" aria-label="Filtros de rentabilidad">
      <label>Desde<input type="date" formControlName="desde" /></label>
      <label>Hasta<input type="date" formControlName="hasta" /></label>

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

      <button type="submit" [disabled]="form.invalid || rangoInvalido || cargandoSelectores || form.disabled">Aplicar filtros</button>

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
  styles: [
    `.filter-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(160px,1fr));gap:.75rem;align-items:end}`,
    `.filter-grid label{display:grid;gap:.25rem}`,
    `.filter-grid input,.filter-grid select,.filter-grid button{min-height:2.5rem;max-width:100%}`,
    `.status,.error{grid-column:1/-1;margin:0}`,
    `.error{color:#b91c1c}`,
  ],
})
export class RentabilidadFiltrosComponent {
  private readonly fb = inject(FormBuilder);

  @Input() opciones: RentabilidadFiltroOpciones = {};
  @Input() cargandoSelectores = false;
  @Input() errorSelectores = '';

  @Input() set disabled(val: boolean) {
    if (val) {
      this.form.disable();
    } else {
      this.form.enable();
    }
  }

  @Output() readonly filterChanged = new EventEmitter<ReporteVentasFiltroDto>();

  readonly form = this.fb.group({
    desde: [''],
    hasta: [''],
    vendedorId: [null as number | null],
    clienteId: [null as number | null],
    sucursalId: [null as number | null],
    categoriaId: [null as number | null],
    productoId: [null as number | null],
    varianteId: [null as number | null],
    marcaId: [null as number | null],
    modeloId: [null as number | null],
    colorId: [null as number | null],
    tallaId: [null as number | null]
  });

  get selectores(): Array<{ label: string; control: RentabilidadSelectorControl; options: RentabilidadFiltroOpcion[] }> {
    return [
      { label: 'Vendedor', control: 'vendedorId', options: this.opciones.vendedores ?? [] },
      { label: 'Cliente', control: 'clienteId', options: this.opciones.clientes ?? [] },
      { label: 'Sucursal', control: 'sucursalId', options: this.opciones.sucursales ?? [] },
      { label: 'Categoría', control: 'categoriaId', options: this.opciones.categorias ?? [] },
      { label: 'Producto', control: 'productoId', options: this.opciones.productos ?? [] },
      { label: 'Variante', control: 'varianteId', options: this.opciones.variantes ?? [] },
      { label: 'Marca', control: 'marcaId', options: this.opciones.marcas ?? [] },
      { label: 'Modelo', control: 'modeloId', options: this.opciones.modelos ?? [] },
      { label: 'Color', control: 'colorId', options: this.opciones.colores ?? [] },
      { label: 'Talla', control: 'tallaId', options: this.opciones.tallas ?? [] },
    ];
  }

  get rangoInvalido(): boolean {
    const { desde, hasta } = this.form.getRawValue();
    return !!desde && !!hasta && desde > hasta;
  }

  emitir(): void {
    if (this.form.disabled || this.form.invalid || this.rangoInvalido || this.cargandoSelectores) return;
    const value = this.form.getRawValue();
    this.filterChanged.emit({
      page: 1,
      pageSize: 20,
      ...(value.desde ? { desde: value.desde } : {}),
      ...(value.hasta ? { hasta: value.hasta } : {}),
      ...(value.vendedorId ? { vendedorId: value.vendedorId } : {}),
      ...(value.clienteId ? { clienteId: value.clienteId } : {}),
      ...(value.sucursalId ? { sucursalId: value.sucursalId } : {}),
      ...(value.categoriaId ? { categoriaId: value.categoriaId } : {}),
      ...(value.productoId ? { productoId: value.productoId } : {}),
      ...(value.varianteId ? { varianteId: value.varianteId } : {}),
      ...(value.marcaId ? { marcaId: value.marcaId } : {}),
      ...(value.modeloId ? { modeloId: value.modeloId } : {}),
      ...(value.colorId ? { colorId: value.colorId } : {}),
      ...(value.tallaId ? { tallaId: value.tallaId } : {}),
    });
  }
}
