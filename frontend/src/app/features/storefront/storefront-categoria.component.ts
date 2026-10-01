import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { cloudinaryResponsiveSrcset, cloudinaryResponsiveUrl } from '../../shared/cloudinary-image.util';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { CategoriaTienda, ModeloTienda, ProductoTienda, crearCatalogoEjemplo, etiquetaDisponibilidad, mapearProducto, mapearProductoResumen, precioVenta } from './storefront.catalog';
import { StorefrontCarritoService } from './storefront-carrito.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './storefront-categorias.catalog';
import { StorefrontHeaderComponent } from './storefront-header.component';
import { EstadoConsultaPublica, EstadoRecursoPublico } from './storefront.models';
import { STOREFRONT_CONFIG } from './storefront.config';
import { STOREFRONT_PATHS } from './storefront.paths';
import { StorefrontSeoService } from './storefront-seo.service';
import { StorefrontService } from './storefront.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './storefront.visual';

@Component({
  selector: 'app-storefront-categoria',
  standalone: true,
  imports: [CommonModule, StorefrontHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './storefront-categoria.component.html',
  styleUrl: './storefront-categoria.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontCategoriaComponent implements OnInit {
  imagenCloudinary(url: string | null | undefined, width = 800): string {
    return cloudinaryResponsiveUrl(url, width);
  }
  srcsetCloudinary(url: string | null | undefined): string | null {
    return cloudinaryResponsiveSrcset(url);
  }

  private readonly servicio = inject(StorefrontService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly seo = inject(StorefrontSeoService);
  readonly identidad = inject(StorefrontIdentidadService);
  readonly config = inject(STOREFRONT_CONFIG);
  readonly carrito = inject(StorefrontCarritoService);

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly permiteWhatsapp = this.config.modoCarrito !== 'tarjeta';
  readonly busqueda = signal('');
  readonly categoria = signal<CategoriaTienda | null>(null);
  readonly estado = signal<EstadoRecursoPublico>('loading');
  readonly error = signal('');
  readonly slugSolicitado = signal('');
  readonly totalUnidadesCarrito = computed<number | null>(() => this.carrito.listo() ? this.carrito.totalUnidades() : null);
  readonly subtotalCarrito = computed<number | null>(() => this.carrito.listo() ? this.carrito.subtotal() : null);
  readonly aviso = signal('');
  readonly catalogo = signal<ProductoTienda[]>([]);
  readonly cargandoProductos = signal(true);
  readonly errorProductos = signal('');
  readonly imagenesConError = signal(new Set<string>());

  readonly productosCategoria = computed(() => {
    const categoria = this.categoria();
    if (!categoria) return [];
    return this.catalogo()
      .filter(producto => producto.activo
        && (producto.categoriaId !== null
          ? producto.categoriaId === categoria.id
          : producto.categoria === categoria.nombre))
      .slice(0, 8);
  });

  readonly estadoProductos = computed<EstadoConsultaPublica>(() => {
    if (this.cargandoProductos() || this.estado() === 'loading') return 'loading';
    if (this.errorProductos()) return 'error';
    return this.productosCategoria().length ? 'success' : 'empty';
  });

  readonly rutaCatalogoCategoria = computed(() => {
    const slug = this.categoria()?.slug;
    return slug ? `${STOREFRONT_PATHS.productos}?categoria=${encodeURIComponent(slug)}` : STOREFRONT_PATHS.productos;
  });

  readonly enlaces = {
    inicio: STOREFRONT_PATHS.inicio,
    categorias: STOREFRONT_PATHS.categorias,
    productos: STOREFRONT_PATHS.productos,
    contacto: `${STOREFRONT_PATHS.inicio}#contacto`
  } as const;

  private cargaCategoria?: Subscription;
  private cargaProductos?: Subscription;
  private cargaCarrito?: Subscription;

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.cargarContextoCarrito());
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      this.slugSolicitado.set((params.get('slug') || '').trim());
      this.cargarCategoria();
    });
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.carrito.reiniciarContexto();
    this.cargarCategoria();
    this.cargarContextoCarrito();
  }

  recargar(): void {
    this.cargarCategoria();
    this.cargarContextoCarrito();
  }

  actualizarBusqueda(texto: string): void { this.busqueda.set(texto.slice(0, 180)); }

  buscar(): void {
    const q = this.busqueda().trim();
    void this.router.navigate(['/tienda/productos'], { queryParams: q ? { q } : {} });
  }

  seleccionarCategoria(nombre: string): void {
    if (!nombre) void this.router.navigate(['/tienda/productos']);
  }

  abrirCarrito(): void { void this.router.navigateByUrl(STOREFRONT_PATHS.carrito); }

  textoCantidad(): string {
    const cantidad = this.categoria()?.cantidadProductos;
    if (cantidad === null || cantidad === undefined) return 'Cantidad no disponible';
    return `${cantidad} ${cantidad === 1 ? 'producto' : 'productos'}`;
  }

  modeloVisible(producto: ProductoTienda): ModeloTienda {
    return producto.modelos.find(modelo => modelo.disponible) ?? producto.modelos[0];
  }

  precioProducto(producto: ProductoTienda): number {
    const modelo = this.modeloVisible(producto);
    return modelo ? precioVenta(producto, modelo) : producto.precio;
  }

  disponibilidadProducto(producto: ProductoTienda): string {
    const modelo = this.modeloVisible(producto);
    return modelo ? etiquetaDisponibilidad(modelo) : 'Agotado';
  }

  imagenProducto(producto: ProductoTienda): string {
    const modelo = this.modeloVisible(producto);
    return modelo?.imagenes[0] || producto.imagenes[0] || '';
  }

  imagenValida(url: string): boolean {
    return Boolean(url) && !this.imagenesConError().has(url);
  }

  errorImagen(url: string): void {
    if (!url || this.imagenesConError().has(url)) return;
    const errores = new Set(this.imagenesConError());
    errores.add(url);
    this.imagenesConError.set(errores);
  }

  rutaProducto(producto: ProductoTienda): string {
    return producto.slug ? STOREFRONT_PATHS.producto(producto.slug) : STOREFRONT_PATHS.productos;
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

  recargarProductos(): void {
    const categoria = this.categoria();
    if (categoria) this.cargarProductosCategoria(categoria);
    this.cargarContextoCarrito();
  }

  private cargarCategoria(): void {
    this.cargaCategoria?.unsubscribe();
    this.categoria.set(null);
    this.error.set('');
    this.estado.set('loading');
    const slug = this.slugSolicitado();
    if (!slug) {
      this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
      this.estado.set('not-found');
      return;
    }

    if (!this.utilizarDatosBaseDatos()) {
      const categoria = crearCategoriasTiendaEjemplo().find(item => item.slug === slug) || null;
      this.categoria.set(categoria);
      if (categoria) {
        this.seo.aplicarCategoria(categoria, this.identidad.config().nombreComercial || 'Tienda');
        this.estado.set('success');
        this.cargarProductosCategoria(categoria);
      } else {
        this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
        this.estado.set('not-found');
      }
      return;
    }

    this.cargaCategoria = this.servicio.obtenerCategoriaPorSlug(slug).pipe(
      map(mapearCategoriaTienda),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: categoria => {
        this.categoria.set(categoria);
        this.seo.aplicarCategoria(categoria, this.identidad.config().nombreComercial || 'Tienda');
        this.estado.set('success');
        this.cargarProductosCategoria(categoria);
        if (categoria.slug !== slug) void this.router.navigateByUrl(STOREFRONT_PATHS.categoria(categoria.slug), { replaceUrl: true });
      },
      error: error => {
        if (this.esNoEncontrada(error)) {
          this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
          this.estado.set('not-found');
          return;
        }
        this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
        this.error.set('No pudimos cargar esta categoría. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.estado.set('error');
      }
    });
  }

  private esNoEncontrada(error: unknown): boolean {
    if (error instanceof HttpErrorResponse) return error.status === 404;
    return error instanceof Error && ['Categoría no encontrada.', 'Slug de categoría no válido.'].includes(error.message);
  }

  private cargarProductosCategoria(categoria: CategoriaTienda): void {
    this.cargaProductos?.unsubscribe();
    this.catalogo.set([]);
    this.cargandoProductos.set(true);
    this.errorProductos.set('');

    const fuente: Observable<ProductoTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerProductos(1, 8, {
          categoriaId: categoria.id,
          sortBy: 'Nombre',
          sortDirection: 'asc'
        }).pipe(map(res => {
          if (!res.success || !res.data || !Array.isArray(res.data.items)) {
            throw new Error('Respuesta de productos de categoría no válida.');
          }
          return res.data.items.map(mapearProductoResumen);
        }))
      : of(crearCatalogoEjemplo()
          .filter(producto => producto.categoriaId !== null
            ? producto.categoriaId === categoria.id
            : producto.categoria === categoria.nombre)
          .slice(0, 8));

    this.cargaProductos = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: productos => {
        this.catalogo.set(productos);
        this.cargandoProductos.set(false);
      },
      error: () => {
        this.cargandoProductos.set(false);
        this.errorProductos.set('No pudimos cargar los productos de esta categoría. Intenta nuevamente.');
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
      error: () => {
        this.aviso.set('No pudimos actualizar el resumen del carrito en esta página. Tu selección sigue guardada.');
      }
    });
  }
}
