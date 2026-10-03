import { Component, HostListener, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProductoService } from '../../services/producto.service';
import { Producto, ProductoImagen } from '../../core/models/producto.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';
import { descargarBlobSeguro } from '../../shared/descarga-segura';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-producto-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, ProductoImagenComponent],
  templateUrl: './producto-detail.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './producto-detail.component.scss'
})
export class ProductoDetailComponent implements OnInit {
  readonly producto = signal<Producto | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly imagenAmpliada = signal<ProductoImagen | null>(null);
  readonly descargando = signal<number | null>(null); // id de imagen en descarga, o -1 para "todas"
  readonly puedeExportar = signal(false);
  readonly puedeEditar = signal(false);
  readonly ajustandoStock = signal(false);

  constructor(
    private productoService: ProductoService,
    private route: ActivatedRoute,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeExportar.set(this.permisosRuntime.puede('Productos', 'Exportar'));
    this.puedeEditar.set(this.permisosRuntime.puede('Productos', 'Editar'));

    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.cargarProducto(id);
  }

  private cargarProducto(id: number): void {
    this.loading.set(true);
    this.productoService.getById(id).subscribe({
      next: (res) => {
        this.producto.set(res.data);
        this.notFound.set(false);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  async ajustarStockProducto(): Promise<void> {
    const producto = this.producto();
    if (!producto || producto.usaVariantes || this.ajustandoStock()) return;

    const cantidadTexto = await this.alerts.solicitarTexto({
      titulo: 'Ajustar inventario',
      mensaje: `Stock actual: ${producto.cantidad}. Ingresa la nueva cantidad para este producto.`,
      tipo: 'advertencia',
      confirmarTexto: 'Continuar',
      entrada: { etiqueta: 'Nueva cantidad', valor: String(producto.cantidad), requerida: true }
    });
    if (cantidadTexto === null) return;

    const cantidadNueva = Number(cantidadTexto.trim());
    if (!Number.isInteger(cantidadNueva) || cantidadNueva < 0) {
      this.snackBar.open('La nueva cantidad debe ser un entero mayor o igual que cero.', 'Cerrar', { duration: 5000 });
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
      this.snackBar.open('El motivo del ajuste es obligatorio.', 'Cerrar', { duration: 5000 });
      return;
    }

    this.ajustandoStock.set(true);
    this.productoService.ajustarStockProducto(producto.id, {
      cantidadActualEsperada: producto.cantidad,
      cantidadNueva,
      motivo
    }).subscribe({
      next: () => {
        this.ajustandoStock.set(false);
        this.snackBar.open('Inventario ajustado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargarProducto(producto.id);
      },
      error: (err) => {
        this.ajustandoStock.set(false);
        this.snackBar.open(
          err.error?.message ?? 'No se pudo ajustar el inventario.',
          'Cerrar',
          { duration: 6000 }
        );
        this.cargarProducto(producto.id);
      }
    });
  }

  ampliar(imagen: ProductoImagen): void {
    this.imagenAmpliada.set(imagen);
  }

  @HostListener('document:keydown.escape')
  cerrarAmpliada(): void {
    this.imagenAmpliada.set(null);
  }

  private guardarBlob(blob: Blob, nombreSugerido: string): void {
    if (!descargarBlobSeguro(blob, nombreSugerido, 'imagen-producto')) {
      this.snackBar.open('El archivo descargado está vacío.', 'Cerrar', { duration: 5000 });
    }
  }

  descargarImagen(imagen: ProductoImagen): void {
    const producto = this.producto();
    if (!producto) return;

    this.descargando.set(imagen.id);
    this.productoService.descargarImagen(producto.id, imagen.id).subscribe({
      next: (blob) => {
        this.descargando.set(null);
        this.guardarBlob(blob, `${producto.nombre}-${imagen.orden + 1}.jpg`);
      },
      error: () => {
        this.descargando.set(null);
        this.snackBar.open('No se pudo descargar la imagen. El archivo podría ya no estar disponible.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  descargarTodas(): void {
    const producto = this.producto();
    if (!producto) return;

    this.descargando.set(-1);
    this.productoService.descargarTodasLasImagenes(producto.id).subscribe({
      next: (blob) => {
        this.descargando.set(null);
        this.guardarBlob(blob, `${producto.nombre}-imagenes.zip`);
      },
      error: () => {
        this.descargando.set(null);
        this.snackBar.open('No se pudieron descargar las imágenes.', 'Cerrar', { duration: 5000 });
      }
    });
  }
}
