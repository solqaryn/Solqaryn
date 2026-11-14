import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { CatalogoProducto } from '../../core/models/catalogo-producto.model';
import { ProductoVarianteFormValue } from '../../core/models/producto.model';

interface CombinacionPreview extends ProductoVarianteFormValue {
  etiqueta: string;
  clave: string;
}

const MAX_COMBINACIONES = 100;

@Component({
  selector: 'app-producto-combination-generator',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatFormFieldModule,
    MatIconModule, MatInputModule, MatSelectModule
  ],
  templateUrl: './producto-combination-generator.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './producto-combination-generator.component.scss'
})
export class ProductoCombinationGeneratorComponent {
  private readonly fb = inject(FormBuilder);

  @Input({ required: true }) marcas: CatalogoProducto[] = [];
  @Input({ required: true }) modelos: CatalogoProducto[] = [];
  @Input({ required: true }) colores: CatalogoProducto[] = [];
  @Input({ required: true }) tallas: CatalogoProducto[] = [];
  @Input() combinacionesExistentes: string[] = [];
  @Output() combinacionesConfirmadas = new EventEmitter<ProductoVarianteFormValue[]>();

  readonly preview = signal<CombinacionPreview[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly omitidasDuplicadas = signal(0);
  readonly maxCombinaciones = MAX_COMBINACIONES;

  readonly form = this.fb.group({
    marcaId: [null as number | null],
    modeloIds: [[] as number[]],
    colorIds: [[] as number[]],
    tallaIds: [[] as number[]],
    cantidad: [0, [Validators.required, Validators.min(0)]],
    umbralStockBajo: [5, [Validators.required, Validators.min(0)]],
    costo: [0, [Validators.required, Validators.min(0)]],
    precio: [0, [Validators.required, Validators.min(0.01)]]
  });

  modelosDeMarca(): CatalogoProducto[] {
    const marcaId = Number(this.form.controls.marcaId.value);
    if (!marcaId) return [];
    return this.modelos.filter(modelo => modelo.catalogoPadreId === marcaId && modelo.activo);
  }

  onMarcaChange(): void {
    const permitidos = new Set(this.modelosDeMarca().map(modelo => modelo.id));
    const vigentes = (this.form.controls.modeloIds.value ?? []).filter(id => permitidos.has(Number(id)));
    this.form.controls.modeloIds.setValue(vigentes);
    this.limpiarPreview();
  }

  generarVistaPrevia(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Completa correctamente cantidad, costo, precio y umbral antes de generar.');
      return;
    }

    const raw = this.form.getRawValue();
    const marcaId = this.normalizarId(raw.marcaId);
    const modeloIds = (raw.modeloIds ?? []).map(id => this.normalizarId(id)).filter((id): id is number => id !== null);
    const colorIds = (raw.colorIds ?? []).map(id => this.normalizarId(id)).filter((id): id is number => id !== null);
    const tallaIds = (raw.tallaIds ?? []).map(id => this.normalizarId(id)).filter((id): id is number => id !== null);

    if (modeloIds.length > 0 && !marcaId) {
      this.errorMessage.set('Selecciona la Marca correspondiente antes de elegir Modelos.');
      return;
    }

    const modelosPermitidos = new Set(this.modelosDeMarca().map(modelo => modelo.id));
    if (modeloIds.some(id => !modelosPermitidos.has(id))) {
      this.errorMessage.set('Uno de los Modelos seleccionados no pertenece a la Marca indicada.');
      return;
    }

    if (!marcaId && modeloIds.length === 0 && colorIds.length === 0 && tallaIds.length === 0) {
      this.errorMessage.set('Selecciona al menos una dimensión para generar variantes.');
      return;
    }

    const bases = modeloIds.length > 0
      ? modeloIds.map(modeloId => ({ marcaId, modeloId }))
      : [{ marcaId, modeloId: null as number | null }];
    const colores = colorIds.length > 0 ? colorIds : [null];
    const tallas = tallaIds.length > 0 ? tallaIds : [null];
    const totalPotencial = bases.length * colores.length * tallas.length;

    if (totalPotencial > MAX_COMBINACIONES) {
      this.errorMessage.set(`La selección produciría ${totalPotencial} combinaciones. Reduce la selección a un máximo de ${MAX_COMBINACIONES} por operación.`);
      return;
    }

    const existentes = new Set(this.combinacionesExistentes);
    const nuevas: CombinacionPreview[] = [];
    let omitidas = 0;

    for (const base of bases) {
      for (const colorId of colores) {
        for (const tallaId of tallas) {
          const clave = this.clave(base.marcaId, base.modeloId, colorId, tallaId);
          if (existentes.has(clave) || nuevas.some(x => x.clave === clave)) {
            omitidas++;
            continue;
          }

          nuevas.push({
            marcaId: base.marcaId,
            modeloId: base.modeloId,
            colorId,
            tallaId,
            cantidad: Number(raw.cantidad),
            umbralStockBajo: Number(raw.umbralStockBajo),
            costo: Number(raw.costo),
            precio: Number(raw.precio),
            activo: true,
            etiqueta: this.etiqueta(base.marcaId, base.modeloId, colorId, tallaId),
            clave
          });
        }
      }
    }

    this.preview.set(nuevas);
    this.omitidasDuplicadas.set(omitidas);
    this.errorMessage.set(nuevas.length === 0 ? 'Todas las combinaciones seleccionadas ya existen en el formulario.' : null);
  }

  confirmar(): void {
    const filas = this.preview();
    if (filas.length === 0) return;
    this.combinacionesConfirmadas.emit(filas.map(({ etiqueta, clave, ...variante }) => variante));
    this.preview.set([]);
    this.omitidasDuplicadas.set(0);
    this.errorMessage.set(null);
  }

  limpiarPreview(): void {
    this.preview.set([]);
    this.omitidasDuplicadas.set(0);
    this.errorMessage.set(null);
  }

  private etiqueta(marcaId: number | null, modeloId: number | null, colorId: number | null, tallaId: number | null): string {
    const partes = [
      this.marcas.find(x => x.id === marcaId)?.nombre,
      this.modelos.find(x => x.id === modeloId)?.nombre,
      this.colores.find(x => x.id === colorId)?.nombre,
      this.tallas.find(x => x.id === tallaId)?.nombre
    ].filter((parte): parte is string => !!parte);
    return partes.join(' · ');
  }

  private clave(marcaId: number | null, modeloId: number | null, colorId: number | null, tallaId: number | null): string {
    return `${marcaId ?? 0}:${modeloId ?? 0}:${colorId ?? 0}:${tallaId ?? 0}`;
  }

  private normalizarId(valor: unknown): number | null {
    const numero = Number(valor);
    return Number.isInteger(numero) && numero > 0 ? numero : null;
  }
}
