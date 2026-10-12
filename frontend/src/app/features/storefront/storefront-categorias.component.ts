
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { cloudinaryResponsiveSrcset, cloudinaryResponsiveUrl } from '../../shared/cloudinary-image.util';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import {
  CategoriaTienda,
  EstadoConsultaPublica,
  ProductoTienda,
  crearCatalogoEjemplo,
  mapearProducto
} from './storefront.catalog';
import { StorefrontCarritoService } from './storefront-carrito.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './storefront-categorias.catalog';
import { StorefrontHeaderComponent } from './storefront-header.component';
import { STOREFRONT_CONFIG } from './storefront.config';
import { STOREFRONT_PATHS } from './storefront.paths';
import { StorefrontService } from './storefront.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './storefront.visual';

@Component({
  selector: 'app-storefront-categorias',
  standalone: true,
  imports: [StorefrontHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './storefront-categorias.component.html',
  styleUrl: './storefront-categorias.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontCategoriasComponent implements OnInit {
  imagenCloudinary(url: string | null | undefined, width = 800): string {
    return cloudinaryResponsiveUrl(url, width);
  }
  srcsetCloudinary(url: string | null | undefined): string | null {
    return cloudinaryResponsiveSrcset(url);
  }

  private readonly servicio = inject(StorefrontService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  readonly identidad = inject(StorefrontIdentidadService);
  readonly config = inject(STOREFRONT_CONFIG);
  readonly carrito = inject(StorefrontCarritoService);

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
    inicio: STOREFRONT_PATHS.inicio,
    categorias: STOREFRONT_PATHS.categorias,
    productos: STOREFRONT_PATHS.productos,
    contacto: `${STOREFRONT_PATHS.inicio}#contacto`
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
    void this.router.navigate(['/tienda/productos'], { queryParams: q ? { q } : {} });
  }

  seleccionarCategoria(nombre: string): void {
    if (!nombre) {
      void this.router.navigate(['/tienda/productos']);
      return;
    }
    const categoria = this.categorias().find(item => item.nombre === nombre);
    if (!categoria) {
      this.aviso.set('La categoría seleccionada ya no está disponible.');
      return;
    }
    void this.router.navigateByUrl(STOREFRONT_PATHS.categoria(categoria.slug));
  }

  abrirCarrito(): void {
    void this.router.navigateByUrl(STOREFRONT_PATHS.carrito);
  }

  rutaExplorar(categoria: CategoriaTienda): string {
    return STOREFRONT_PATHS.categoria(categoria.slug);
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
