import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { cloudinaryResponsiveSrcset, cloudinaryResponsiveUrl } from '../../shared/cloudinary-image.util';
import { VaristorehnIdentidadService } from './varistorehn-identidad.service';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { VaristorehnService } from './varistorehn.service';
import { ModoCarrito, VARISTOREHN_CONFIG } from './varistorehn.config';
import {
  CategoriaTienda,
  EstadoConsultaPublica,
  ModeloTienda,
  ProductoTienda,
  crearCatalogoEjemplo,
  mapearProductoResumen,
  telefonoWhatsapp
} from './varistorehn.catalog';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './varistorehn-categorias.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

@Component({
  selector: 'app-varistorehn',
  standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn.component.html',
  styleUrls: ['./varistorehn.component.scss', './varistorehn.responsive.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnComponent implements OnInit {
  imagenCloudinary(url: string | null | undefined, width = 800): string {
    return cloudinaryResponsiveUrl(url, width);
  }
  srcsetCloudinary(url: string | null | undefined): string | null {
    return cloudinaryResponsiveSrcset(url);
  }

  private readonly servicio = inject(VaristorehnService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly identidad = inject(VaristorehnIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);
  readonly carritoStore = inject(VaristorehnCarritoService);

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly modoCarrito = signal<ModoCarrito>(this.config.modoCarrito);
  readonly permiteWhatsapp = computed(() => this.modoCarrito() !== 'tarjeta');
  readonly permiteTarjeta = computed(() => this.modoCarrito() !== 'whatsapp');
  readonly telefono = computed(() => telefonoWhatsapp(this.identidad.config().whatsApp));
  readonly tarjetaConfigurada = Boolean(this.config.endpointCheckoutTarjeta && this.config.origenesCheckoutPermitidos.length);

  readonly busqueda = signal('');
  readonly aviso = signal('');
  readonly imagenesFallidas = signal<Set<string>>(new Set());

  readonly categoriasTienda = signal<CategoriaTienda[]>([]);
  readonly cargandoCategorias = signal(true);
  readonly errorCategorias = signal('');
  readonly estadoCategorias = computed<EstadoConsultaPublica>(() => {
    if (this.cargandoCategorias()) return 'loading';
    if (this.errorCategorias()) return 'error';
    return this.categoriasTienda().length ? 'success' : 'empty';
  });
  readonly categoriasPortada = computed(() => this.categoriasTienda().slice(0, 6));
  readonly categoriasNavegacion = computed(() => this.categoriasTienda().map(categoria => categoria.nombre));

  readonly destacados = signal<ProductoTienda[]>([]);
  readonly cargandoDestacados = signal(true);
  readonly errorDestacados = signal('');
  readonly estadoDestacados = computed<EstadoConsultaPublica>(() => {
    if (this.cargandoDestacados()) return 'loading';
    if (this.errorDestacados()) return 'error';
    return this.destacados().length ? 'success' : 'empty';
  });
  readonly destacadoPrincipal = computed(() => this.destacados()[0] || null);

  readonly totalUnidades = computed<number | null>(() =>
    this.carritoStore.listo() ? this.carritoStore.totalUnidades() : null
  );
  readonly totalCarrito = computed<number | null>(() =>
    this.carritoStore.listo() ? this.carritoStore.subtotal() : null
  );

  readonly enlaces = {
    productos: VARISTOREHN_PATHS.productos,
    categorias: VARISTOREHN_PATHS.categorias,
    carrito: VARISTOREHN_PATHS.carrito,
    contacto: `${VARISTOREHN_PATHS.inicio}#contacto`
  } as const;

  private cargaCategoriasActual?: Subscription;
  private cargaDestacadosActual?: Subscription;

  ngOnInit(): void {
    const query = this.route.snapshot.queryParamMap;
    if (query.get('carrito') === '1') {
      void this.router.navigateByUrl(VARISTOREHN_PATHS.carrito, { replaceUrl: true });
      return;
    }

    const busquedaLegada = (query.get('q') || '').trim().slice(0, 180);
    const categoriaLegada = (query.get('categoria') || '').trim().slice(0, 180);
    if (busquedaLegada || categoriaLegada) {
      void this.router.navigate([VARISTOREHN_PATHS.productos], {
        queryParams: {
          ...(busquedaLegada ? { q: busquedaLegada } : {}),
          ...(categoriaLegada ? { categoria: categoriaLegada } : {})
        },
        replaceUrl: true
      });
      return;
    }

    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((bootstrap) => {
      if (!this.utilizarDatosBaseDatos()) {
        this.categoriasTienda.set(crearCategoriasTiendaEjemplo());
        this.destacados.set(
          crearCatalogoEjemplo()
            .filter(producto => producto.activo && producto.destacado && Boolean(producto.slug))
            .slice(0, 4)
        );
        this.cargandoCategorias.set(false);
        this.cargandoDestacados.set(false);
        return;
      }

      if (!bootstrap) {
        this.cargarCategorias();
        this.cargarDestacados();
        return;
      }

      try {
        this.categoriasTienda.set(bootstrap.categorias.map(mapearCategoriaTienda));
        this.destacados.set(
          bootstrap.destacados
            .map(mapearProductoResumen)
            .filter(producto => producto.activo && producto.destacado && Boolean(producto.slug))
            .slice(0, 4)
        );
        this.cargandoCategorias.set(false);
        this.cargandoDestacados.set(false);
      } catch {
        this.errorCategorias.set('La tienda devolvió categorías con un formato no válido.');
        this.errorDestacados.set('La tienda devolvió destacados con un formato no válido.');
        this.cargandoCategorias.set(false);
        this.cargandoDestacados.set(false);
      }
    });
  }

  cargarCategorias(): void {
    this.cargaCategoriasActual?.unsubscribe();
    this.cargandoCategorias.set(true);
    this.errorCategorias.set('');
    this.categoriasTienda.set([]);

    const fuente: Observable<CategoriaTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCategorias().pipe(map(categorias => categorias.map(mapearCategoriaTienda)))
      : of(crearCategoriasTiendaEjemplo());

    this.cargaCategoriasActual = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categorias => {
        this.categoriasTienda.set(categorias);
        this.cargandoCategorias.set(false);
      },
      error: () => {
        this.errorCategorias.set('No pudimos cargar las categorías. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.cargandoCategorias.set(false);
      }
    });
  }

  cargarDestacados(): void {
    this.cargaDestacadosActual?.unsubscribe();
    this.cargandoDestacados.set(true);
    this.errorDestacados.set('');
    this.destacados.set([]);

    const fuente: Observable<ProductoTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerDestacados(4).pipe(map(productos => productos.map(mapearProductoResumen)))
      : of(
          crearCatalogoEjemplo()
            .filter(producto => producto.activo && producto.destacado && Boolean(producto.slug))
            .slice(0, 4)
        );

    this.cargaDestacadosActual = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: productos => {
        this.destacados.set(productos.filter(producto => producto.activo && producto.destacado && Boolean(producto.slug)).slice(0, 4));
        this.cargandoDestacados.set(false);
      },
      error: () => {
        this.errorDestacados.set('No pudimos cargar los productos destacados. Revisa la conexión e intenta de nuevo; no se sustituyeron datos reales por ejemplos.');
        this.cargandoDestacados.set(false);
      }
    });
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.busqueda.set('');
    this.aviso.set('');
    this.cargarCategorias();
    this.cargarDestacados();
  }

  cambiarModo(modo: string): void {
    if (!this.controlesVistaPrevia || !['whatsapp', 'tarjeta', 'ambos'].includes(modo)) return;
    this.modoCarrito.set(modo as ModoCarrito);
  }

  actualizarBusqueda(texto: string): void {
    this.busqueda.set(texto.slice(0, 180));
  }

  buscarCatalogo(): void {
    const texto = this.busqueda().trim();
    void this.router.navigate([VARISTOREHN_PATHS.productos], {
      queryParams: texto ? { q: texto } : undefined
    });
  }

  seleccionarCategoria(nombre: string): void {
    const categoria = this.categoriasTienda().find(item => item.nombre === nombre);
    if (!categoria) {
      this.aviso.set('La categoría seleccionada ya no está disponible.');
      return;
    }
    void this.router.navigateByUrl(VARISTOREHN_PATHS.categoria(categoria.slug));
  }

  abrirCategoria(categoria: CategoriaTienda): void {
    if (!categoria.slug) return;
    void this.router.navigateByUrl(VARISTOREHN_PATHS.categoria(categoria.slug));
  }

  abrirDetalle(producto: ProductoTienda): void {
    if (!producto.slug) {
      this.aviso.set('Este producto todavía no tiene una URL pública disponible.');
      return;
    }
    void this.router.navigateByUrl(VARISTOREHN_PATHS.producto(producto.slug));
  }

  abrirProductos(): void {
    void this.router.navigateByUrl(VARISTOREHN_PATHS.productos);
  }

  abrirCarrito(): void {
    void this.router.navigateByUrl(VARISTOREHN_PATHS.carrito);
  }

  textoCantidadCategoria(categoria: CategoriaTienda): string {
    const cantidad = categoria.cantidadProductos;
    if (cantidad === null) return 'Explorar categoría';
    return `${cantidad} ${cantidad === 1 ? 'producto' : 'productos'}`;
  }

  modeloSeleccionado(producto: ProductoTienda): ModeloTienda {
    return producto.modelos.find(modelo => modelo.disponible) || producto.modelos[0];
  }

  fotos(producto: ProductoTienda): string[] {
    return this.modeloSeleccionado(producto)?.imagenes || producto.imagenes;
  }

  imagenValida(url?: string): boolean {
    return Boolean(url && !this.imagenesFallidas().has(url));
  }

  errorImagen(url: string): void {
    if (!url) return;
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

  enlaceWhatsapp(): string {
    return this.telefono() ? `https://wa.me/${this.telefono()}` : '';
  }
}
