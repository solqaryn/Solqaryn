import { Component, OnInit, inject, signal } from '@angular/core';

import { ReactiveFormsModule, FormArray, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProductoService } from '../../services/producto.service';
import { CategoriaService } from '../../services/categoria.service';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { Categoria } from '../../core/models/categoria.model';
import { CatalogoProducto } from '../../core/models/catalogo-producto.model';
import { ProductoImagen, ProductoVariante, ProductoVarianteFormValue, TipoInventario } from '../../core/models/producto.model';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';
import { ProductoCombinationGeneratorComponent } from './producto-combination-generator.component';

interface ImagenPreview {
  id?: number;
  url: string;
  esPrincipal: boolean;
  archivo?: File;
}

const MAX_IMAGENES = 5;

@Component({
  selector: 'app-producto-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressSpinnerModule,
    ProductoImagenComponent,
    ProductoCombinationGeneratorComponent
],
  templateUrl: './producto-form.component.html',
  styleUrls: ['./producto-form.component.scss', './producto-form-variants.component.scss']
})
export class ProductoFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly categorias = signal<Categoria[]>([]);
  readonly colores = signal<CatalogoProducto[]>([]);
  readonly tallas = signal<CatalogoProducto[]>([]);
  readonly marcas = signal<CatalogoProducto[]>([]);
  readonly modelos = signal<CatalogoProducto[]>([]);
  readonly modelosTodos = signal<CatalogoProducto[]>([]);
  readonly imagenes = signal<ImagenPreview[]>([]);
  readonly maxImagenes = MAX_IMAGENES;
  readonly auditoria = signal<{ creadoPor?: string; actualizadoPor?: string } | null>(null);
  readonly TipoInventario = TipoInventario;
  readonly tiposInventario = [
    { value: TipoInventario.MercaderiaVenta, label: 'Mercadería para venta' },
    { value: TipoInventario.InsumoAdministrativo, label: 'Insumo administrativo' }
  ];

  productoId: number | null = null;
  private imagenesAEliminarIds: number[] = [];

  form = this.fb.group({
    nombre: ['', Validators.required],
    tipoInventario: [TipoInventario.MercaderiaVenta, Validators.required],
    marcaId: [null as number | null],
    modeloId: [null as number | null],
    descripcion: [''],
    categoriaId: [null as number | null],
    esDestacado: [false],
    variantes: this.fb.array<any>([])
  });

  constructor(
    private productoService: ProductoService,
    private categoriaService: CategoriaService,
    private catalogoService: CatalogoProductoService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  get variantes(): FormArray { return this.form.controls.variantes; }
  get stockTotal(): number { return this.variantes.getRawValue().reduce((total, variante) => total + Number(variante.cantidad ?? 0), 0); }
  get umbralTotal(): number { return this.variantes.getRawValue().reduce((total, variante) => total + Number(variante.umbralStockBajo ?? 0), 0); }

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      categorias: this.categoriaService.getActivas(),
      colores: this.catalogoService.getActivos('Color'),
      tallas: this.catalogoService.getActivos('Talla'),
      marcas: this.catalogoService.getActivos('Marca'),
      modelos: this.catalogoService.getAll('Modelo')
    }).subscribe({
      next: (res) => {
        this.categorias.set(res.categorias.data);
        this.colores.set(res.colores.data);
        this.tallas.set(res.tallas.data);
        this.marcas.set(res.marcas.data);
        this.modelosTodos.set(res.modelos.data.filter(modelo => modelo.activo));
        const idParam = this.route.snapshot.paramMap.get('id');
        if (idParam) {
          this.isEdit.set(true);
          this.productoId = Number(idParam);
          this.cargarProducto(this.productoId);
        } else {
          this.agregarVariante();
          this.loading.set(false);
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudieron cargar los catálogos necesarios para el producto.');
      }
    });

    this.form.controls.marcaId.valueChanges.subscribe((marcaId) => {
      this.form.controls.modeloId.setValue(null, { emitEvent: false });
      this.actualizarModelosCabecera(marcaId);

      if (!this.isEdit() && marcaId) {
        for (const control of this.variantes.controls) {
          if (!this.normalizarId(control.get('marcaId')?.value)) {
            control.get('marcaId')?.setValue(marcaId);
            control.get('modeloId')?.setValue(null);
          }
        }
      }
    });

    this.form.controls.modeloId.valueChanges.subscribe((modeloId) => {
      if (this.isEdit() || !modeloId) return;
      const marcaId = this.normalizarId(this.form.controls.marcaId.value);
      if (!marcaId) return;

      for (const control of this.variantes.controls) {
        const marcaVarianteId = this.normalizarId(control.get('marcaId')?.value);
        const modeloVarianteId = this.normalizarId(control.get('modeloId')?.value);
        if (marcaVarianteId === marcaId && !modeloVarianteId)
          control.get('modeloId')?.setValue(modeloId);
      }
    });
  }

  private crearVarianteGroup(variante?: Partial<ProductoVarianteFormValue>) {
    return this.fb.group({
      id: [variante?.id ?? null as number | null],
      marcaId: [variante?.marcaId ?? null as number | null],
      modeloId: [variante?.modeloId ?? null as number | null],
      colorId: [variante?.colorId ?? null as number | null],
      tallaId: [variante?.tallaId ?? null as number | null],
      sku: [variante?.sku ?? '', Validators.maxLength(80)],
      codigoBarras: [variante?.codigoBarras ?? '', Validators.maxLength(120)],
      cantidad: [variante?.cantidad ?? 0, [Validators.required, Validators.min(0)]],
      umbralStockBajo: [variante?.umbralStockBajo ?? 5, [Validators.required, Validators.min(0)]],
      costo: [variante?.costo ?? 0, [Validators.required, Validators.min(0)]],
      precio: [variante?.precio ?? 0, [Validators.required, Validators.min(0.01)]],
      activo: [variante?.activo ?? true]
    });
  }

  agregarVariante(): void {
    const primera = this.variantes.length > 0 ? this.variantes.at(0).getRawValue() : null;
    const marcaPredeterminada = this.form.controls.marcaId.value ?? primera?.marcaId ?? null;
    const modeloPredeterminado = this.form.controls.modeloId.value ?? primera?.modeloId ?? null;
    this.variantes.push(this.crearVarianteGroup({
      marcaId: marcaPredeterminada,
      modeloId: modeloPredeterminado,
      cantidad: 0,
      umbralStockBajo: primera?.umbralStockBajo ?? 5,
      costo: primera?.costo ?? 0,
      precio: primera?.precio ?? 0,
      activo: true
    }));
    this.errorMessage.set(null);
  }

  get combinacionesActuales(): string[] {
    return this.variantes.getRawValue().map(variante => this.claveCombinacion({
      marcaId: this.normalizarId(variante.marcaId),
      modeloId: this.normalizarId(variante.modeloId),
      colorId: this.normalizarId(variante.colorId),
      tallaId: this.normalizarId(variante.tallaId)
    }));
  }

  agregarCombinacionesGeneradas(generadas: ProductoVarianteFormValue[]): void {
    if (generadas.length === 0) return;

    const inicial = this.variantes.length === 1 ? this.variantes.at(0).getRawValue() : null;
    const esFilaInicialVacia = inicial && !inicial.id &&
      !this.normalizarId(inicial.marcaId) && !this.normalizarId(inicial.modeloId) &&
      !this.normalizarId(inicial.colorId) && !this.normalizarId(inicial.tallaId) &&
      !String(inicial.sku ?? '').trim() && !String(inicial.codigoBarras ?? '').trim() &&
      Number(inicial.cantidad ?? 0) === 0 && Number(inicial.costo ?? 0) === 0 && Number(inicial.precio ?? 0) === 0;
    if (esFilaInicialVacia) this.variantes.clear();

    const existentes = new Set(this.combinacionesActuales);
    let agregadas = 0;
    let omitidas = 0;
    for (const variante of generadas) {
      const clave = this.claveCombinacion(variante);
      if (existentes.has(clave)) {
        omitidas++;
        continue;
      }
      this.variantes.push(this.crearVarianteGroup(variante));
      existentes.add(clave);
      agregadas++;
    }

    this.errorMessage.set(omitidas > 0
      ? `${agregadas} combinación(es) agregada(s); ${omitidas} duplicada(s) fueron omitidas.`
      : null);
  }

  private claveCombinacion(variante: Pick<ProductoVarianteFormValue, 'marcaId' | 'modeloId' | 'colorId' | 'tallaId'>): string {
    return `${variante.marcaId ?? 0}:${variante.modeloId ?? 0}:${variante.colorId ?? 0}:${variante.tallaId ?? 0}`;
  }

  quitarVariante(index: number): void {
    if (this.variantes.length === 1) {
      this.errorMessage.set('El producto debe conservar al menos una variante física con su existencia.');
      return;
    }
    const variante = this.variantes.at(index).getRawValue();
    if (variante.id && Number(variante.cantidad) > 0) {
      this.errorMessage.set('No puedes quitar una variante existente mientras tenga unidades. Ajusta primero su stock a cero.');
      return;
    }
    this.variantes.removeAt(index);
    this.errorMessage.set(null);
  }

  modelosDeMarca(marcaId: number | null | undefined): CatalogoProducto[] {
    if (!marcaId) return [];
    return this.modelosTodos().filter(modelo => modelo.catalogoPadreId === Number(marcaId));
  }

  onMarcaVarianteChange(index: number): void {
    const grupo = this.variantes.at(index);
    const marcaId = this.normalizarId(grupo.get('marcaId')?.value);
    const modeloId = this.normalizarId(grupo.get('modeloId')?.value);
    if (modeloId && !this.modelosDeMarca(marcaId).some(modelo => modelo.id === modeloId)) {
      grupo.get('modeloId')?.setValue(null);
    }
  }

  etiquetaVariante(index: number): string {
    const raw = this.variantes.at(index).getRawValue();
    const partes = [
      this.marcas().find(x => x.id === this.normalizarId(raw.marcaId))?.nombre,
      this.modelosTodos().find(x => x.id === this.normalizarId(raw.modeloId))?.nombre,
      this.colores().find(x => x.id === this.normalizarId(raw.colorId))?.nombre,
      this.tallas().find(x => x.id === this.normalizarId(raw.tallaId))?.nombre,
      raw.sku?.trim()?.toUpperCase()
    ].filter(Boolean);
    return partes.length > 0 ? partes.join(' · ') : 'Define al menos una dimensión';
  }

  private cargarProducto(id: number): void {
    this.productoService.getById(id).subscribe({
      next: (res) => {
        const p = res.data;
        const marcaId = p.marcaId ?? null;
        this.actualizarModelosCabecera(marcaId);
        const modeloId = p.modeloId && this.modelos().some(m => m.id === p.modeloId) ? p.modeloId : null;
        this.form.patchValue({
          nombre: p.nombre,
          tipoInventario: p.tipoInventario ?? TipoInventario.MercaderiaVenta,
          marcaId,
          modeloId,
          descripcion: p.descripcion,
          categoriaId: p.categoriaId ?? null,
          esDestacado: p.esDestacado === true
        }, { emitEvent: false });

        this.variantes.clear();
        if ((p.variantes ?? []).some(variante => !variante.esTecnica)) {
          p.variantes
            .filter(variante => !variante.esTecnica)
            .forEach((variante: ProductoVariante) => this.variantes.push(this.crearVarianteGroup({
              id: variante.id,
              marcaId: variante.marcaId,
              modeloId: variante.modeloId,
              colorId: variante.colorId,
              tallaId: variante.tallaId,
              sku: variante.sku,
              codigoBarras: variante.codigoBarras ?? undefined,
              cantidad: variante.cantidad,
              umbralStockBajo: variante.umbralStockBajo,
              costo: variante.costo,
              precio: variante.precio,
              activo: variante.activo
            })));
        } else {
          this.variantes.push(this.crearVarianteGroup({
            marcaId: p.marcaId,
            modeloId: p.modeloId,
            colorId: p.colorId,
            tallaId: p.tallaId,
            cantidad: p.cantidad,
            umbralStockBajo: p.umbralStockBajo,
            costo: p.costo,
            precio: p.precio,
            activo: true
          }));
        }

        if (this.isEdit()) this.variantes.controls.forEach((control) => control.disable({ emitEvent: false }));
        this.imagenes.set((p.imagenes ?? []).filter(img => img.productoVarianteId == null).map((img: ProductoImagen) => ({ id: img.id, url: img.url, esPrincipal: img.esPrincipal })));
        this.auditoria.set({ creadoPor: p.creadoPorNombreUsuario, actualizadoPor: p.actualizadoPorNombreUsuario });
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('No se pudo cargar el producto.');
      }
    });
  }

  private actualizarModelosCabecera(marcaId: number | null): void {
    this.modelos.set(this.modelosDeMarca(marcaId));
  }

  get espaciosDisponibles(): number { return this.maxImagenes - this.imagenes().length; }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (files.length === 0) return;
    const disponibles = this.espaciosDisponibles;
    if (files.length > disponibles) this.errorMessage.set(`Solo puedes agregar ${disponibles} foto(s) más (máximo ${this.maxImagenes}).`);
    const nuevas: ImagenPreview[] = files.slice(0, disponibles).map((archivo) => ({
      url: URL.createObjectURL(archivo),
      esPrincipal: this.imagenes().length === 0,
      archivo
    }));
    this.imagenes.set([...this.imagenes(), ...nuevas]);
    input.value = '';
  }

  quitarImagen(index: number): void {
    const actuales = [...this.imagenes()];
    const [quitada] = actuales.splice(index, 1);
    if (quitada.id) this.imagenesAEliminarIds.push(quitada.id);
    if (quitada.archivo) URL.revokeObjectURL(quitada.url);
    if (quitada.esPrincipal && actuales.length > 0) actuales[0].esPrincipal = true;
    this.imagenes.set(actuales);
  }

  marcarComoPrincipal(index: number): void {
    this.imagenes.set(this.imagenes().map((img, i) => ({ ...img, esPrincipal: i === index })));
  }

  submit(): void {
    if (this.form.invalid || this.variantes.length === 0) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Completa los datos obligatorios y agrega al menos una variante física.');
      return;
    }

    const variantes: ProductoVarianteFormValue[] = this.variantes.getRawValue().map((variante) => ({
      id: variante.id ?? undefined,
      marcaId: this.normalizarId(variante.marcaId),
      modeloId: this.normalizarId(variante.modeloId),
      colorId: this.normalizarId(variante.colorId),
      tallaId: this.normalizarId(variante.tallaId),
      sku: variante.sku?.trim()?.toUpperCase() || undefined,
      codigoBarras: variante.codigoBarras?.trim() || undefined,
      cantidad: Number(variante.cantidad),
      umbralStockBajo: Number(variante.umbralStockBajo),
      costo: Number(variante.costo),
      precio: Number(variante.precio),
      activo: variante.activo !== false
    }));

    if (variantes.some(v => !v.marcaId && !v.modeloId && !v.colorId && !v.tallaId)) {
      this.errorMessage.set('Cada variante debe definir al menos Marca, Modelo, Color o Talla.');
      return;
    }

    if (variantes.some(v => v.modeloId && !v.marcaId)) {
      this.errorMessage.set('Toda variante con Modelo debe indicar también su Marca.');
      return;
    }

    const combinaciones = variantes.map(v => `${v.marcaId ?? 0}:${v.modeloId ?? 0}:${v.colorId ?? 0}:${v.tallaId ?? 0}`);
    if (new Set(combinaciones).size !== combinaciones.length) {
      this.errorMessage.set('No puedes registrar dos veces la misma combinación de Marca, Modelo, Color y Talla.');
      return;
    }

    const skus = variantes.map(v => v.sku).filter((sku): sku is string => !!sku);
    if (new Set(skus).size !== skus.length) {
      this.errorMessage.set('No puedes repetir un SKU entre las variantes del producto.');
      return;
    }

    const codigos = variantes.map(v => v.codigoBarras).filter((codigo): codigo is string => !!codigo);
    if (new Set(codigos).size !== codigos.length) {
      this.errorMessage.set('No puedes repetir un código de barras entre las variantes del producto.');
      return;
    }

    const marcaComunId = this.valorComun(variantes.map(v => v.marcaId ?? null));
    const modeloComunId = this.valorComun(variantes.map(v => v.modeloId ?? null));
    const colorComunId = this.valorComun(variantes.map(v => v.colorId ?? null));
    const tallaComunId = this.valorComun(variantes.map(v => v.tallaId ?? null));
    const marca = this.marcas().find(item => item.id === marcaComunId);
    const modelo = this.modelosTodos().find(item => item.id === modeloComunId);
    const total = variantes.reduce((suma, variante) => suma + variante.cantidad, 0);
    const costo = total > 0
      ? variantes.reduce((suma, variante) => suma + variante.costo * variante.cantidad, 0) / total
      : variantes[0].costo;
    const preciosActivos = variantes.filter(variante => variante.activo).map(variante => variante.precio);
    const precio = preciosActivos.length > 0 ? Math.min(...preciosActivos) : variantes[0].precio;
    const imagenPrincipal = this.imagenes().find(img => img.esPrincipal);

    this.saving.set(true);
    this.errorMessage.set(null);
    const value = {
      nombre: this.form.value.nombre!,
      tipoInventario: Number(this.form.value.tipoInventario) as TipoInventario,
      marca: marca?.nombre ?? '',
      modelo: modelo?.nombre ?? '',
      marcaId: marcaComunId,
      modeloId: modeloComunId,
      colorId: colorComunId,
      tallaId: tallaComunId,
      descripcion: this.form.value.descripcion || undefined,
      esDestacado: this.form.value.esDestacado === true,
      cantidad: total,
      costo: Math.round(costo * 100) / 100,
      precio,
      umbralStockBajo: variantes.reduce((suma, variante) => suma + variante.umbralStockBajo, 0),
      categoriaId: this.form.value.categoriaId,
      variantes,
      imagenesNuevas: this.imagenes().filter(img => img.archivo).map(img => img.archivo!),
      imagenesAEliminarIds: this.imagenesAEliminarIds,
      imagenPrincipalId: imagenPrincipal?.id ?? null
    };

    const request$ = this.isEdit() ? this.productoService.update(this.productoId!, value) : this.productoService.create(value);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/productos']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el producto y sus variantes.');
      }
    });
  }

  private normalizarId(valor: unknown): number | null {
    const numero = Number(valor);
    return Number.isInteger(numero) && numero > 0 ? numero : null;
  }

  private valorComun(valores: Array<number | null>): number | null {
    const unicos = [...new Set(valores)];
    return unicos.length === 1 ? unicos[0] : null;
  }
}
