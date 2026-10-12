import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CatalogoProducto } from '../../core/models/catalogo-producto.model';
import { Producto, ProductoImagen, ProductoVariante, ProductoVarianteFormValue } from '../../core/models/producto.model';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { ProductoService } from '../../services/producto.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { descargarBlobSeguro } from '../../shared/descarga-segura';

const MAX_IMAGENES_VARIANTE = 5;

@Component({
  selector: 'app-producto-variantes',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, MatButtonModule, MatFormFieldModule,
    MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, MatTableModule,
    ProductoImagenComponent
  ],
  templateUrl: './producto-variantes.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './producto-variantes.component.scss'
})
export class ProductoVariantesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productoService = inject(ProductoService);
  private readonly catalogoService = inject(CatalogoProductoService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly alerts = inject(AppAlertService);
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly producto = signal<Producto | null>(null);
  readonly variantes = signal<ProductoVariante[]>([]);
  readonly marcas = signal<CatalogoProducto[]>([]);
  readonly modelos = signal<CatalogoProducto[]>([]);
  readonly colores = signal<CatalogoProducto[]>([]);
  readonly tallas = signal<CatalogoProducto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly ajustandoId = signal<number | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly editandoId = signal<number | null>(null);
  readonly imagenVariante = signal<ProductoVariante | null>(null);
  readonly imagenesVariante = signal<ProductoImagen[]>([]);
  readonly cargandoImagenes = signal(false);
  readonly subiendoImagenes = signal(false);
  readonly descargandoImagenId = signal<number | null>(null);
  readonly puedeExportar = signal(false);
  readonly displayedColumns = ['variante', 'sku', 'codigoBarras', 'stock', 'costo', 'precio', 'estado', 'acciones'];
  readonly maxImagenesVariante = MAX_IMAGENES_VARIANTE;
  productoId = 0;

  readonly form = this.fb.group({
    marcaId: [null as number | null],
    modeloId: [null as number | null],
    colorId: [null as number | null],
    tallaId: [null as number | null],
    sku: ['', Validators.maxLength(80)],
    codigoBarras: ['', Validators.maxLength(120)],
    cantidad: [0, [Validators.required, Validators.min(0)]],
    umbralStockBajo: [5, [Validators.required, Validators.min(0)]],
    costo: [0, [Validators.required, Validators.min(0)]],
    precio: [0, [Validators.required, Validators.min(0.01)]]
  });

  ngOnInit(): void {
    this.puedeExportar.set(this.permisosRuntime.puede('Productos', 'Exportar'));
    this.productoId = Number(this.route.snapshot.paramMap.get('id'));
    forkJoin({
      marcas: this.catalogoService.getActivos('Marca'),
      modelos: this.catalogoService.getAll('Modelo'),
      colores: this.catalogoService.getActivos('Color'),
      tallas: this.catalogoService.getActivos('Talla')
    }).subscribe({
      next: (res) => {
        this.marcas.set(res.marcas.data);
        this.modelos.set(res.modelos.data.filter(modelo => modelo.activo));
        this.colores.set(res.colores.data);
        this.tallas.set(res.tallas.data);
      },
      error: () => this.errorMessage.set('No se pudieron cargar los catálogos de variantes.')
    });
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.productoService.getById(this.productoId).subscribe({
      next: (res) => {
        this.producto.set(res.data);
        this.variantes.set(res.data.variantes ?? []);
        const actual = this.imagenVariante();
        if (actual) {
          const refrescada = (res.data.variantes ?? []).find(v => v.id === actual.id) ?? null;
          this.imagenVariante.set(refrescada);
          if (refrescada) this.cargarImagenesVariante(refrescada, false);
        }
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('No se pudo cargar el producto.');
        this.loading.set(false);
      }
    });
  }

  modelosDeMarca(marcaId: number | null | undefined): CatalogoProducto[] {
    if (!marcaId) return [];
    return this.modelos().filter(modelo => modelo.catalogoPadreId === Number(marcaId));
  }

  onMarcaChange(): void {
    const marcaId = this.form.controls.marcaId.value;
    const modeloId = this.form.controls.modeloId.value;
    if (modeloId && !this.modelosDeMarca(marcaId).some(modelo => modelo.id === modeloId)) {
      this.form.controls.modeloId.setValue(null);
    }
  }

  editar(variante: ProductoVariante): void {
    if (variante.esTecnica) {
      this.errorMessage.set('La variante técnica representa un producto simple. Convierte el producto a variantes desde Editar producto.');
      return;
    }
    this.editandoId.set(variante.id);
    this.form.controls.cantidad.enable({ emitEvent: false });
    this.form.setValue({
      marcaId: variante.marcaId ?? null,
      modeloId: variante.modeloId ?? null,
      colorId: variante.colorId ?? null,
      tallaId: variante.tallaId ?? null,
      sku: variante.sku,
      codigoBarras: variante.codigoBarras ?? '',
      cantidad: variante.cantidad,
      umbralStockBajo: variante.umbralStockBajo,
      costo: variante.costo,
      precio: variante.precio
    });
    this.form.controls.cantidad.disable({ emitEvent: false });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  cancelar(): void {
    this.editandoId.set(null);
    this.form.controls.cantidad.enable({ emitEvent: false });
    this.form.reset({
      marcaId: null,
      modeloId: null,
      colorId: null,
      tallaId: null,
      sku: '',
      codigoBarras: '',
      cantidad: 0,
      umbralStockBajo: 5,
      costo: 0,
      precio: 0
    });
  }

  async ajustarStock(variante: ProductoVariante): Promise<void> {
    if (this.ajustandoId() !== null) return;

    const cantidadTexto = await this.alerts.solicitarTexto({
      titulo: 'Ajustar inventario de variante',
      mensaje: `Stock actual de ${variante.etiqueta || variante.sku}: ${variante.cantidad}. Ingresa la nueva cantidad.`,
      tipo: 'advertencia',
      confirmarTexto: 'Continuar',
      entrada: { etiqueta: 'Nueva cantidad', valor: String(variante.cantidad), requerida: true }
    });
    if (cantidadTexto === null) return;

    const cantidadNueva = Number(cantidadTexto.trim());
    if (!Number.isInteger(cantidadNueva) || cantidadNueva < 0) {
      this.errorMessage.set('La nueva cantidad debe ser un entero mayor o igual que cero.');
      return;
    }

    const motivo = await this.alerts.solicitarTexto({
      titulo: 'Motivo del ajuste',
      mensaje: 'Registra un motivo para mantener la trazabilidad del cambio de inventario.',
      tipo: 'advertencia',
      confirmarTexto: 'Aplicar ajuste',
      entrada: { etiqueta: 'Motivo obligatorio', requerida: true }
    });
    if (!motivo) {
      this.errorMessage.set('El motivo del ajuste es obligatorio.');
      return;
    }

    this.ajustandoId.set(variante.id);
    this.errorMessage.set(null);
    this.productoService.ajustarStockVariante(this.productoId, variante.id, {
      cantidadActualEsperada: variante.cantidad,
      cantidadNueva,
      motivo
    }).subscribe({
      next: () => {
        this.ajustandoId.set(null);
        this.snackBar.open('Inventario de la variante exacta ajustado correctamente.', 'Cerrar', { duration: 3500 });
        this.cancelar();
        this.cargar();
      },
      error: (err) => {
        this.ajustandoId.set(null);
        this.errorMessage.set(err.error?.message ?? 'No se pudo ajustar el inventario de la variante.');
        this.cargar();
      }
    });
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const value: ProductoVarianteFormValue = {
      marcaId: raw.marcaId || null,
      modeloId: raw.modeloId || null,
      colorId: raw.colorId || null,
      tallaId: raw.tallaId || null,
      sku: raw.sku?.trim()?.toUpperCase() || undefined,
      codigoBarras: raw.codigoBarras?.trim() || undefined,
      cantidad: Number(raw.cantidad),
      umbralStockBajo: Number(raw.umbralStockBajo),
      costo: Number(raw.costo),
      precio: Number(raw.precio)
    };

    if (!value.marcaId && !value.modeloId && !value.colorId && !value.tallaId) {
      this.errorMessage.set('Define al menos una dimensión: Marca, Modelo, Color o Talla.');
      return;
    }
    if (value.modeloId && !value.marcaId) {
      this.errorMessage.set('Para utilizar Modelo debes seleccionar su Marca.');
      return;
    }

    const duplicada = this.variantes().some(v =>
      !v.esTecnica &&
      v.id !== this.editandoId() &&
      (v.marcaId ?? null) === value.marcaId &&
      (v.modeloId ?? null) === value.modeloId &&
      (v.colorId ?? null) === value.colorId &&
      (v.tallaId ?? null) === value.tallaId);
    if (duplicada) {
      this.errorMessage.set('Ya existe una variante con la misma combinación de Marca, Modelo, Color y Talla.');
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    const request$ = this.editandoId()
      ? this.productoService.actualizarVariante(this.productoId, this.editandoId()!, value)
      : this.productoService.crearVariante(this.productoId, value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelar();
        this.snackBar.open('Variante multidimensional guardada correctamente.', 'Cerrar', { duration: 3000 });
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la variante.');
      }
    });
  }

  cambiarEstado(variante: ProductoVariante): void {
    if (variante.esTecnica) return;
    this.productoService.cambiarEstadoVariante(this.productoId, variante.id, !variante.activo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.errorMessage.set(err.error?.message ?? 'No se pudo cambiar el estado.')
    });
  }

  async eliminar(variante: ProductoVariante): Promise<void> {
    if (variante.esTecnica) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar variante',
      mensaje: `¿Eliminar lógicamente la variante ${variante.etiqueta || variante.sku}? Solo es posible con stock cero.`,
      tipo: 'peligro',
      confirmarTexto: 'Eliminar variante'
    });
    if (!confirmado) return;
    this.productoService.eliminarVariante(this.productoId, variante.id).subscribe({
      next: () => {
        if (this.imagenVariante()?.id === variante.id) this.cerrarImagenesVariante();
        this.snackBar.open('Variante eliminada lógicamente.', 'Cerrar', { duration: 3000 });
        this.cargar();
      },
      error: (err) => this.errorMessage.set(err.error?.message ?? 'No se pudo eliminar la variante.')
    });
  }

  gestionarImagenes(variante: ProductoVariante): void {
    this.imagenVariante.set(variante);
    this.cargarImagenesVariante(variante, true);
  }

  cerrarImagenesVariante(): void {
    this.imagenVariante.set(null);
    this.imagenesVariante.set([]);
    this.cargandoImagenes.set(false);
  }

  cargarImagenesVariante(variante: ProductoVariante, desplazar: boolean): void {
    this.cargandoImagenes.set(true);
    this.productoService.getImagenesVariante(this.productoId, variante.id).subscribe({
      next: (res) => {
        this.imagenesVariante.set(res.data ?? []);
        this.cargandoImagenes.set(false);
        if (desplazar) {
          setTimeout(() => document.getElementById('galeria-variante')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
        }
      },
      error: (err) => {
        this.cargandoImagenes.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudieron cargar las imágenes de la variante.');
      }
    });
  }

  get imagenesSonFallback(): boolean {
    const imagenes = this.imagenesVariante();
    return imagenes.length > 0 && imagenes.every(imagen => imagen.productoVarianteId == null);
  }

  get imagenesEspecificasCount(): number {
    return this.imagenesVariante().filter(imagen => imagen.productoVarianteId != null).length;
  }

  get espaciosImagenesVariante(): number {
    return Math.max(0, this.maxImagenesVariante - this.imagenesEspecificasCount);
  }

  onImagenesVarianteSelected(event: Event): void {
    const variante = this.imagenVariante();
    const input = event.target as HTMLInputElement;
    const archivos = Array.from(input.files ?? []);
    input.value = '';
    if (!variante || archivos.length === 0) return;
    if (variante.esTecnica) {
      this.errorMessage.set('La variante técnica usa la galería general del producto.');
      return;
    }

    const seleccion = archivos.slice(0, this.espaciosImagenesVariante);
    if (seleccion.length === 0) {
      this.errorMessage.set(`La variante ya alcanzó el máximo de ${this.maxImagenesVariante} imágenes.`);
      return;
    }
    if (archivos.length > seleccion.length) {
      this.errorMessage.set(`Solo se cargarán ${seleccion.length} imagen(es); el máximo por variante es ${this.maxImagenesVariante}.`);
    }

    this.subiendoImagenes.set(true);
    this.productoService.agregarImagenesVariante(this.productoId, variante.id, seleccion).subscribe({
      next: (res) => {
        this.imagenesVariante.set(res.data ?? []);
        this.subiendoImagenes.set(false);
        this.errorMessage.set(null);
        this.snackBar.open('Galería específica de la variante actualizada.', 'Cerrar', { duration: 3000 });
      },
      error: (err) => {
        this.subiendoImagenes.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudieron subir las imágenes de la variante.');
      }
    });
  }

  descargarImagenVariante(imagen: ProductoImagen): void {
    const producto = this.producto();
    const variante = this.imagenVariante();
    if (!producto || !variante || !this.puedeExportar() || this.descargandoImagenId() !== null) return;

    this.descargandoImagenId.set(imagen.id);
    this.productoService.descargarImagen(this.productoId, imagen.id).subscribe({
      next: (blob) => {
        this.descargandoImagenId.set(null);
        const etiqueta = (variante.etiqueta || variante.sku || 'variante')
          .replace(/[^a-zA-Z0-9_-]+/g, '_');
        const extension = blob.type.includes('png') ? 'png' : blob.type.includes('webp') ? 'webp' : 'jpg';
        const nombre = `${producto.nombre}-${etiqueta}-${imagen.orden + 1}.${extension}`;
        if (!descargarBlobSeguro(blob, nombre, 'imagen-variante')) {
          this.snackBar.open('El archivo descargado está vacío.', 'Cerrar', { duration: 5000 });
        }
      },
      error: () => {
        this.descargandoImagenId.set(null);
        this.snackBar.open('No se pudo descargar la imagen de la variante.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  marcarPrincipalVariante(imagen: ProductoImagen): void {
    const variante = this.imagenVariante();
    if (!variante || imagen.productoVarianteId == null || imagen.esPrincipal) return;
    this.productoService.marcarImagenPrincipalVariante(this.productoId, variante.id, imagen.id).subscribe({
      next: () => this.cargarImagenesVariante(variante, false),
      error: (err) => this.errorMessage.set(err.error?.message ?? 'No se pudo cambiar la imagen principal de la variante.')
    });
  }

  async eliminarImagenVariante(imagen: ProductoImagen): Promise<void> {
    const variante = this.imagenVariante();
    if (!variante || imagen.productoVarianteId == null) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar imagen de variante',
      mensaje: '¿Eliminar esta imagen de la variante? La imagen general del producto seguirá disponible como respaldo.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar imagen'
    });
    if (!confirmado) return;
    this.productoService.eliminarImagenVariante(this.productoId, variante.id, imagen.id).subscribe({
      next: () => {
        this.snackBar.open('Imagen de variante eliminada.', 'Cerrar', { duration: 2500 });
        this.cargarImagenesVariante(variante, false);
      },
      error: (err) => this.errorMessage.set(err.error?.message ?? 'No se pudo eliminar la imagen de la variante.')
    });
  }
}
