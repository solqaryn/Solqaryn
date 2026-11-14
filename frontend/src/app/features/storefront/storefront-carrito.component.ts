
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { cloudinaryResponsiveSrcset, cloudinaryResponsiveUrl } from '../../shared/cloudinary-image.util';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { CategoriaTienda, ProductoCatalogoPublico, ProductoTienda, crearCatalogoEjemplo, mapearProducto } from './storefront.catalog';
import { StorefrontCarritoService } from './storefront-carrito.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './storefront-categorias.catalog';
import { StorefrontHeaderComponent } from './storefront-header.component';
import { STOREFRONT_CONFIG } from './storefront.config';
import { EstadoConsultaPublica } from './storefront.models';
import { STOREFRONT_PATHS } from './storefront.paths';
import { StorefrontService } from './storefront.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './storefront.visual';

@Component({
  selector: 'app-storefront-carrito',
  standalone: true,
  imports: [StorefrontHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './storefront-carrito.component.html',
  styleUrl: './storefront-carrito.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontCarritoComponent implements OnInit {
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
  readonly busqueda = signal('');
  readonly categorias = signal<CategoriaTienda[]>([]);
  readonly estado = signal<EstadoConsultaPublica>('loading');
  readonly error = signal('');
  readonly aviso = signal('');
  readonly imagenesFallidas = signal<Set<string>>(new Set());
  readonly categoriasNavegacion = computed(() => this.categorias().map(categoria => categoria.nombre));
  readonly totalUnidades = computed<number | null>(() => this.carrito.listo() ? this.carrito.totalUnidades() : null);
  readonly subtotal = computed<number | null>(() => this.carrito.listo() ? this.carrito.subtotal() : null);
  /** Solo en vista previa de dev propagamos explícitamente la fuente elegida entre rutas. */
  readonly enlaceCheckout = computed(() => this.controlesVistaPrevia && this.utilizarDatosBaseDatos()
    ? `${STOREFRONT_PATHS.checkout}?fuente=bd`
    : STOREFRONT_PATHS.checkout);

  readonly enlaces = {
    inicio: STOREFRONT_PATHS.inicio,
    productos: STOREFRONT_PATHS.productos,
    ofertas: STOREFRONT_PATHS.ofertas,
    categorias: STOREFRONT_PATHS.categorias,
    contacto: `${STOREFRONT_PATHS.inicio}#contacto`
  } as const;

  private cargaCatalogo?: Subscription;
  private cargaCategorias?: Subscription;

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.recargar());
  }

  recargar(): void {
    this.cargarCatalogo();
    this.cargarCategoriasPublicas();
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
      void this.router.navigateByUrl(STOREFRONT_PATHS.productos);
      return;
    }
    const categoria = this.categorias().find(item => item.nombre === nombre);
    if (!categoria) return;
    void this.router.navigateByUrl(STOREFRONT_PATHS.categoria(categoria.slug));
  }

  abrirCarrito(): void {
    // Ya estamos en la ruta canónica del carrito.
  }

  incrementar(clave: string): void {
    this.carrito.incrementar(clave);
  }

  disminuir(clave: string): void {
    this.carrito.disminuir(clave);
  }

  establecerCantidad(clave: string, valor: string): void {
    const unidades = Number(valor);
    if (!Number.isFinite(unidades)) return;
    this.carrito.establecerCantidad(clave, unidades);
  }

  quitar(clave: string): void {
    this.carrito.quitar(clave);
    this.aviso.set('Producto retirado del carrito.');
  }

  vaciar(): void {
    this.carrito.vaciar();
    this.aviso.set('El carrito quedó vacío.');
  }

  imagenValida(url?: string): boolean {
    return Boolean(url && !this.imagenesFallidas().has(url));
  }

  errorImagen(url: string): void {
    this.imagenesFallidas.update(actual => new Set([...actual, url]));
  }

  moneda(valor: number): string {
    try {
      return new Intl.NumberFormat('es-HN', {
        style: 'currency',
        currency: this.identidad.config().moneda || 'HNL'
      }).format(valor);
    } catch {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
    }
  }

  private cargarCatalogo(): void {
    this.cargaCatalogo?.unsubscribe();
    this.estado.set('loading');
    this.error.set('');
    this.aviso.set('');
    this.carrito.reiniciarContexto();

    const idsPersistidos = this.carrito.productoIdsPersistidos(
      this.identidad.config().id,
      this.utilizarDatosBaseDatos()
    );
    const fuente: Observable<ProductoCatalogoPublico[] | null> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerProductosContexto(idsPersistidos)
      : of(null);

    this.cargaCatalogo = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: datos => {
        const productos: ProductoTienda[] = datos === null ? crearCatalogoEjemplo() : datos.map(mapearProducto);
        const resultado = this.carrito.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
        if (resultado.ajustado) this.aviso.set(this.carrito.aviso());
        this.estado.set(this.carrito.vacio() ? 'empty' : 'success');
      },
      error: () => {
        this.error.set('No pudimos validar el carrito contra el catálogo actual. Tu selección guardada no fue sustituida ni enviada. Intenta de nuevo.');
        this.estado.set('error');
      }
    });
  }

  private cargarCategoriasPublicas(): void {
    this.cargaCategorias?.unsubscribe();
    this.categorias.set([]);
    const fuente: Observable<CategoriaTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCategorias().pipe(map(categorias => categorias.map(mapearCategoriaTienda)))
      : of(crearCategoriasTiendaEjemplo());

    this.cargaCategorias = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categorias => this.categorias.set(categorias),
      error: () => this.aviso.set('No pudimos actualizar las categorías del encabezado; el carrito sigue disponible.')
    });
  }
}
