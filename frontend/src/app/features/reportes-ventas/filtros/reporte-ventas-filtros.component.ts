
import { Component, EventEmitter, Input, Output, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ReporteVentasFiltroDto } from '../../../core/models/reporte-ventas.models';

export interface ReporteVentasFiltroOpcion {
  id: number;
  nombre: string;
}

export interface ReporteVentasFiltroOpciones {
  vendedores?: ReporteVentasFiltroOpcion[];
  clientes?: ReporteVentasFiltroOpcion[];
  sucursales?: ReporteVentasFiltroOpcion[];
  categorias?: ReporteVentasFiltroOpcion[];
  productos?: ReporteVentasFiltroOpcion[];
  variantes?: ReporteVentasFiltroOpcion[];
  marcas?: ReporteVentasFiltroOpcion[];
  modelos?: ReporteVentasFiltroOpcion[];
  colores?: ReporteVentasFiltroOpcion[];
  tallas?: ReporteVentasFiltroOpcion[];
}

type ReporteVentasSelectorControl =
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
  selector: 'app-reporte-ventas-filtros',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form class="filter-grid" [formGroup]="form" (ngSubmit)="emitir()" aria-label="Filtros del reporte de ventas">
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

      <label>
        Tamaño de página
        <select formControlName="pageSize">
          <option [ngValue]="20">20</option>
          <option [ngValue]="50">50</option>
          <option [ngValue]="100">100</option>
        </select>
      </label>

      <button type="submit" [disabled]="form.invalid || rangoInvalido || cargandoSelectores">Aplicar filtros</button>

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
export class ReporteVentasFiltrosComponent {
  private readonly fb = inject(FormBuilder);

  @Input() opciones: ReporteVentasFiltroOpciones = {};
  @Input() cargandoSelectores = false;
  @Input() errorSelectores = '';
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
    tallaId: [null as number | null],
    pageSize: [20, [Validators.required, Validators.min(1), Validators.max(100)]],
  });

  get selectores(): Array<{ label: string; control: ReporteVentasSelectorControl; options: ReporteVentasFiltroOpcion[] }> {
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
    if (this.form.invalid || this.rangoInvalido || this.cargandoSelectores) return;
    const value = this.form.getRawValue();
    this.filterChanged.emit({
      page: 1,
      pageSize: value.pageSize ?? 20,
      sortBy: 'Fecha',
      sortDirection: 'desc',
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
