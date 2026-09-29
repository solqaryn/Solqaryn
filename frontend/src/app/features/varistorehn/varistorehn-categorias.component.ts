import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { VaristorehnIdentidadService } from './varistorehn-identidad.service';
import {
  CategoriaTienda,
  EstadoConsultaPublica,
  ProductoTienda,
  crearCatalogoEjemplo,
  mapearProducto
} from './varistorehn.catalog';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './varistorehn-categorias.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { VARISTOREHN_CONFIG } from './varistorehn.config';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { VaristorehnService } from './varistorehn.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

@Component({
  selector: 'app-varistorehn-categorias',
  standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn-categorias.component.html',
  styleUrl: './varistorehn-categorias.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnCategoriasComponent implements OnInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  readonly identidad = inject(VaristorehnIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);
  readonly carrito = inject(VaristorehnCarritoService);

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly permiteWhatsapp = this.config.modoCarrito !== 'tarjeta';
  readonly busqueda = signal('');
  readonly categorias = signal<CategoriaTienda[]>([]);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly estado = computed<EstadoConsultaPublica>(() => {
    if (this.cargando()) return 'loading';
    if (this.error()) return 'error';
    return this.categorias().length ? 'success' : 'empty';
  });
  readonly totalUnidadesCarrito = computed<number | null>(() => this.carrito.listo() ? this.carrito.totalUnidades() : null);
  readonly subtotalCarrito = computed<number | null>(() => this.carrito.listo() ? this.carrito.subtotal() : null);
  readonly aviso = signal('');
  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    categorias: VARISTOREHN_PATHS.categorias,
    productos: VARISTOREHN_PATHS.productos,
    contacto: `${VARISTOREHN_PATHS.inicio}#contacto`
  } as const;

  private cargaCategorias?: Subscription;
  private cargaCarrito?: Subscription;

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.recargar());
  }

  recargar(): void {
    this.cargarCategorias();
    this.cargarContextoCarrito();
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.carrito.reiniciarContexto();
    this.recargar();
  }

  actualizarBusqueda(texto: string): void {
    this.busqueda.set(texto.slice(0, 180));
  }

  buscar(): void {
    const q = this.busqueda().trim();
    void this.router.navigate(['/varistorehn/productos'], { queryParams: q ? { q } : {} });
  }

  seleccionarCategoria(nombre: string): void {
    if (!nombre) {
      void this.router.navigate(['/varistorehn/productos']);
      return;
    }
    const categoria = this.categorias().find(item => item.nombre === nombre);
    if (!categoria) {
      this.aviso.set('La categoría seleccionada ya no está disponible.');
      return;
    }
    void this.router.navigateByUrl(VARISTOREHN_PATHS.categoria(categoria.slug));
  }

  abrirCarrito(): void {
    void this.router.navigateByUrl(VARISTOREHN_PATHS.carrito);
  }

  rutaExplorar(categoria: CategoriaTienda): string {
    return VARISTOREHN_PATHS.categoria(categoria.slug);
  }

  textoCantidad(categoria: CategoriaTienda): string {
    if (categoria.cantidadProductos === null) return 'Cantidad no disponible';
    return `${categoria.cantidadProductos} ${categoria.cantidadProductos === 1 ? 'producto' : 'productos'}`;
  }

  imagenValida(categoria: CategoriaTienda): boolean {
    return Boolean(categoria.imagenUrl?.trim());
  }

  private cargarCategorias(): void {
    this.cargaCategorias?.unsubscribe();
    this.cargando.set(true);
    this.error.set('');
    this.categorias.set([]);

    const fuente: Observable<CategoriaTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCategorias().pipe(map(categorias => categorias.map(mapearCategoriaTienda)))
      : of(crearCategoriasTiendaEjemplo());

    this.cargaCategorias = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categorias => {
        this.categorias.set(categorias);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar las categorías. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.cargando.set(false);
      }
    });
  }

  private cargarContextoCarrito(): void {
    this.cargaCarrito?.unsubscribe();
    this.carrito.reiniciarContexto();
    const idsPersistidos = this.carrito.productoIdsPersistidos(
      this.identidad.config().id,
      this.utilizarDatosBaseDatos()
    );
    const fuente: Observable<ProductoTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerProductosContexto(idsPersistidos).pipe(map(productos => productos.map(mapearProducto)))
      : of(crearCatalogoEjemplo());

    this.cargaCarrito = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: productos => {
        const resultado = this.carrito.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
        if (resultado.ajustado) this.aviso.set(this.carrito.aviso());
      },
      error: () => this.aviso.set('No pudimos actualizar el resumen del carrito en esta página. Tu selección sigue guardada.')
    });
  }
}
