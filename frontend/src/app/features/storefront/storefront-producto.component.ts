import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { cloudinaryResponsiveSrcset, cloudinaryResponsiveUrl } from '../../shared/cloudinary-image.util';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { construirEnlaceWhatsApp } from '../../core/services/whatsapp-share.service';
import { mensajeWhatsappCompraDirecta } from './storefront-checkout.rules';
import {
  CategoriaTienda,
  ModeloTienda,
  ProductoCatalogoPublico,
  ProductoTienda,
  crearCatalogoEjemplo,
  etiquetaDisponibilidad,
  mapearProducto,
  mapearProductoResumen,
  precioVenta,
  telefonoWhatsapp
} from './storefront.catalog';
import { StorefrontCarritoService } from './storefront-carrito.service';
import { EstadoRecursoPublico } from './storefront.models';
import { StorefrontCuentaService } from './storefront-cuenta.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './storefront-categorias.catalog';
import { StorefrontHeaderComponent } from './storefront-header.component';
import { STOREFRONT_CONFIG } from './storefront.config';
import { STOREFRONT_PATHS } from './storefront.paths';
import { StorefrontSeoService } from './storefront-seo.service';
import { StorefrontService } from './storefront.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './storefront.visual';

interface CaracteristicaPublica { etiqueta: string; valor: string; }
interface ContextoRetornoCatalogo {
  url: string;
  scrollY: number;
  productoId: number;
  modelosActivos: Record<number, string>;
  filtrosAbiertos: boolean;
}

const RETORNO_CATALOGO_STORAGE = 'storefront:retorno-catalogo:v1';

@Component({
  selector: 'app-storefront-producto',
  standalone: true,
  imports: [StorefrontHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './storefront-producto.component.html',
  styleUrl: './storefront-producto.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontProductoComponent implements OnInit {
  private readonly servicio = inject(StorefrontService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly seo = inject(StorefrontSeoService);

  readonly identidad = inject(StorefrontIdentidadService);
  readonly config = inject(STOREFRONT_CONFIG);
  readonly carritoStore = inject(StorefrontCarritoService);
  readonly cuentaCliente = inject(StorefrontCuentaService);

  @ViewChild('lightbox') private lightbox?: ElementRef<HTMLDialogElement>;
  @ViewChild('botonImagenPrincipal') private botonImagenPrincipal?: ElementRef<HTMLButtonElement>;

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly busqueda = signal('');
  readonly slugSolicitado = signal('');
  readonly producto = signal<ProductoTienda | null>(null);
  readonly estado = signal<EstadoRecursoPublico>('loading');
  readonly error = signal('');
  readonly aviso = signal('');
  readonly vistaWhatsapp = signal('');
  readonly categorias = signal<CategoriaTienda[]>([]);
  readonly catalogoContexto = signal<ProductoTienda[]>([]);
  readonly contextoCarritoCargado = this.carritoStore.listo;
  readonly carrito = this.carritoStore.items;
  readonly modeloClave = signal('');
  readonly cantidad = signal(0);
  readonly imagenActiva = signal(0);
  readonly imagenesFallidas = signal<Set<string>>(new Set());
  readonly lightboxAbierto = signal(false);
  readonly favorito = signal(false);
  readonly favoritoProcesando = signal(false);

  readonly telefono = computed(() => telefonoWhatsapp(this.identidad.config().whatsApp));
  readonly permiteWhatsapp = computed(() => this.config.modoCarrito !== 'tarjeta' && Boolean(this.telefono()));
  readonly categoriasNavegacion = computed(() => this.categorias().map(categoria => categoria.nombre));
  readonly categoriaProducto = computed(() => {
    const producto = this.producto();
    if (!producto) return null;
    return this.categorias().find(categoria =>
      (producto.categoriaId !== null && categoria.id === producto.categoriaId) || categoria.nombre === producto.categoria
    ) || null;
  });

  readonly modeloSeleccionado = computed<ModeloTienda | null>(() => {
    const producto = this.producto();
    if (!producto?.modelos.length) return null;
    return producto.modelos.find(modelo => modelo.clave === this.modeloClave())
      || producto.modelos.find(modelo => modelo.disponible)
      || producto.modelos[0];
  });
  readonly imagenes = computed(() => {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    if (!producto) return [];
    return [...new Set([...(modelo?.imagenes || []), ...producto.imagenes].map(url => url.trim()).filter(Boolean))];
  });
  readonly imagenActual = computed(() => this.imagenes()[this.imagenActiva()] || '');
  readonly skuVisible = computed(() => this.modeloSeleccionado()?.sku || this.producto()?.sku || '');
  readonly precioNormal = computed(() => this.modeloSeleccionado()?.precio ?? this.producto()?.precio ?? 0);
  readonly precioActual = computed(() => {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    return producto && modelo ? precioVenta(producto, modelo) : 0;
  });
  readonly tienePromocion = computed(() => this.precioActual() < this.precioNormal());
  readonly ahorroActual = computed(() => Math.max(0, this.precioNormal() - this.precioActual()));
  readonly porcentajeAhorroActual = computed(() =>
    this.precioNormal() > 0 ? Math.round(this.ahorroActual() * 100 / this.precioNormal()) : 0);
  readonly ofertaNombre = computed(() => this.modeloSeleccionado()?.ofertaNombre || 'Oferta vigente');
  readonly stockSeleccionado = computed(() => this.modeloSeleccionado()?.stock ?? 0);
  readonly puedeSeleccionarCantidad = computed(() => Boolean(this.modeloSeleccionado()?.disponible && this.stockRestante() > 0));
  readonly unidadesEnCarrito = computed(() => {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    return producto && modelo ? this.carritoStore.unidadesDe(producto.id, modelo.clave) : 0;
  });
  readonly stockRestante = computed(() => Math.max(0, this.stockSeleccionado() - this.unidadesEnCarrito()));
  readonly puedeAgregar = computed(() => Boolean(
    this.estado() === 'success'
    && this.contextoCarritoCargado()
    && this.producto()?.activo
    && this.modeloSeleccionado()?.disponible
    && this.cantidad() > 0
    && this.cantidad() <= this.stockRestante()
  ));
  readonly totalUnidades = computed<number | null>(() => this.carritoStore.listo() ? this.carritoStore.totalUnidades() : null);
  readonly subtotal = computed<number | null>(() => this.carritoStore.listo() ? this.carritoStore.subtotal() : null);

  readonly caracteristicas = computed<CaracteristicaPublica[]>(() => {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    if (!producto || !modelo) return [];
    return [
      { etiqueta: 'Marca', valor: modelo.marca || producto.marca },
      { etiqueta: 'Modelo', valor: modelo.nombre !== 'Modelo general' ? modelo.nombre : '' },
      { etiqueta: 'SKU', valor: modelo.sku || producto.sku },
      { etiqueta: 'Categoría', valor: producto.categoria }
    ].filter(item => item.valor.trim());
  });
  readonly relacionados = computed(() => {
    const actual = this.producto();
    if (!actual) return [];
    return this.catalogoContexto()
      .filter(producto => producto.activo && producto.id !== actual.id
        && (actual.categoriaId !== null ? producto.categoriaId === actual.categoriaId : producto.categoria === actual.categoria))
      .slice(0, 4);
  });

  readonly enlaces = {
    inicio: STOREFRONT_PATHS.inicio,
    productos: STOREFRONT_PATHS.productos,
    ofertas: STOREFRONT_PATHS.ofertas,
    categorias: STOREFRONT_PATHS.categorias,
    contacto: `${STOREFRONT_PATHS.inicio}#contacto`
  } as const;

  private cargaProducto?: Subscription;
  private cargaCatalogo?: Subscription;
  private cargaRelacionados?: Subscription;
  private cargaCategorias?: Subscription;
  private identidadLista = false;
  private readonly retornoCatalogo = this.leerContextoRetorno();
  private inicioSwipe: { x: number; y: number } | null = null;
  private suprimirClickImagen = false;
  private restaurarFocoLightbox = true;

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      this.slugSolicitado.set((params.get('slug') || '').trim().slice(0, 180));
      if (this.identidadLista) this.cargarProducto();
    });
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.identidadLista = true;
      this.cargarProducto();
      this.cargarContextoCatalogo();
      this.cargarCategorias();
    });
    this.cuentaCliente.restaurar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.sincronizarFavorito());
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.carritoStore.reiniciarContexto();
    this.cantidad.set(0); this.imagenActiva.set(0); this.vistaWhatsapp.set('');
    this.cargarProducto(); this.cargarContextoCatalogo(); this.cargarCategorias();
  }
  recargar(): void { this.cargarProducto(); this.cargarContextoCatalogo(); this.cargarCategorias(); }
  actualizarBusqueda(texto: string): void { this.busqueda.set(texto.slice(0, 180)); }
  buscar(): void {
    const q = this.busqueda().trim();
    void this.router.navigate(['/tienda/productos'], { queryParams: q ? { q } : {} });
  }
  seleccionarCategoria(nombre: string): void {
    if (!nombre) { void this.router.navigate(['/tienda/productos']); return; }
    const categoria = this.categorias().find(item => item.nombre === nombre);
    void this.router.navigate(['/tienda/productos'], { queryParams: categoria ? { categoria: categoria.slug } : {} });
  }
  abrirCarrito(): void { void this.router.navigateByUrl(STOREFRONT_PATHS.carrito); }
  volverAlOrigen(): void {
    const view = this.document.defaultView;
    const retorno = this.retornoCatalogo;

    if (retorno) {
      try {
        view?.sessionStorage.setItem(RETORNO_CATALOGO_STORAGE, JSON.stringify(retorno));
      } catch {
        // El state del Router conserva el contexto si sessionStorage no esta disponible.
      }
      void this.router.navigateByUrl(retorno.url, {
        replaceUrl: true,
        state: { storefrontCatalogState: retorno }
      });
      return;
    }
    if (view && view.history.length > 1 && this.referenciaStorefront(view)) {
      view.history.back();
      return;
    }
    void this.router.navigateByUrl(STOREFRONT_PATHS.productos);
  }
  alternarFavorito(): void {
    const producto = this.producto();
    if (!producto || this.favoritoProcesando()) return;
    if (!this.utilizarDatosBaseDatos()) {
      this.aviso.set('Los favoritos de cuenta usan el catálogo real.');
      return;
    }
    if (!this.cuentaCliente.autenticado()) {
      this.aviso.set('Inicia sesión en Mi cuenta para guardar favoritos.');
      void this.router.navigateByUrl(STOREFRONT_PATHS.cuenta);
      return;
    }

    this.favoritoProcesando.set(true);
    const accion = this.favorito()
      ? this.cuentaCliente.quitarFavorito(producto.id)
      : this.cuentaCliente.agregarFavorito(producto.id);
    accion.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.favorito.update(valor => !valor);
        this.favoritoProcesando.set(false);
        this.aviso.set(this.favorito() ? 'Producto guardado en favoritos.' : 'Producto retirado de favoritos.');
      },
      error: () => {
        this.favoritoProcesando.set(false);
        this.aviso.set('No pudimos actualizar tus favoritos.');
      }
    });
  }

  seleccionarModelo(clave: string): void {
    const producto = this.producto();
    if (!producto?.modelos.some(modelo => modelo.clave === clave)) return;
    this.modeloClave.set(clave); this.imagenActiva.set(0); this.cerrarLightbox(false); this.reiniciarCantidad();
  }
  cambiarCantidad(cambio: number): void {
    if (!Number.isInteger(cambio) || !this.puedeSeleccionarCantidad()) return;
    this.cantidad.set(Math.max(1, Math.min(this.stockRestante(), this.cantidad() + cambio)));
  }
  establecerCantidad(valor: string): void {
    if (!this.puedeSeleccionarCantidad()) { this.cantidad.set(0); return; }
    const numero = Number(valor);
    const cantidad = Number.isFinite(numero) ? Math.floor(numero) : 1;
    this.cantidad.set(Math.max(1, Math.min(this.stockRestante(), cantidad)));
  }
  agregarAlCarrito(): void {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    if (!producto || !modelo || !this.puedeAgregar()) return;
    const agregadas = this.carritoStore.agregar(producto, modelo, this.cantidad());
    if (!agregadas) return;
    this.reiniciarCantidad();
    this.aviso.set(`${agregadas} ${agregadas === 1 ? 'unidad agregada' : 'unidades agregadas'} de ${producto.nombre}.`);
  }
  comprarWhatsapp(): void {
    const producto = this.producto();
    const modelo = this.modeloSeleccionado();
    const telefono = this.telefono();
    if (!producto || !modelo || !telefono || !this.permiteWhatsapp() || !modelo.disponible || modelo.stock <= 0) return;
    const unidades = Math.max(1, Math.min(this.cantidad() || 1, modelo.stock));
    const subtotal = this.precioActual() * unidades;
    const marca = this.identidad.config().nombreComercial || 'Tienda';
    const sku = this.skuVisible();
    const enlaceProducto = this.enlaceProductoActual();
    const mensaje = mensajeWhatsappCompraDirecta(marca, 'HNL', {
      nombre: producto.nombre,
      modelo: modelo.nombre || undefined,
      sku: sku || undefined,
      unidades,
      precioUnitario: this.precioActual(),
      subtotal,
      enlace: enlaceProducto || undefined
    });
    if (!this.utilizarDatosBaseDatos()) { this.vistaWhatsapp.set(`VISTA PREVIA — NO ENVIADO\n\n${mensaje}`); return; }
    const url = construirEnlaceWhatsApp(telefono, mensaje);
    if (!url) { this.aviso.set('El mensaje es demasiado largo o el número no es válido para abrir WhatsApp.'); return; }
    this.document.defaultView?.open(url, '_blank', 'noopener,noreferrer');
  }

  seleccionarImagen(indice: number): void {
    if (Number.isInteger(indice) && indice >= 0 && indice < this.imagenes().length) this.imagenActiva.set(indice);
  }
  moverImagen(cambio: number): void {
    const total = this.imagenes().length;
    if (total <= 1 || !Number.isInteger(cambio)) return;
    this.imagenActiva.set((this.imagenActiva() + cambio + total) % total);
  }
  iniciarSwipe(evento: PointerEvent): void {
    if (evento.pointerType !== 'mouse') this.inicioSwipe = { x: evento.clientX, y: evento.clientY };
  }
  finalizarSwipe(evento: PointerEvent): void {
    if (!this.inicioSwipe || evento.pointerType === 'mouse') return;
    const deltaX = evento.clientX - this.inicioSwipe.x;
    const deltaY = evento.clientY - this.inicioSwipe.y;
    this.inicioSwipe = null;
    if (Math.abs(deltaX) < 48 || Math.abs(deltaX) <= Math.abs(deltaY) * 1.2) return;
    this.moverImagen(deltaX < 0 ? 1 : -1);
    this.suprimirClickImagen = true;
    this.document.defaultView?.setTimeout(() => { this.suprimirClickImagen = false; }, 0);
  }
  cancelarSwipe(): void { this.inicioSwipe = null; }
  abrirLightbox(): void {
    if (this.suprimirClickImagen || !this.imagenValida(this.imagenActual())) return;
    const dialogo = this.lightbox?.nativeElement;
    if (!dialogo || dialogo.open) return;
    this.restaurarFocoLightbox = true; dialogo.showModal(); this.lightboxAbierto.set(true);
  }
  cerrarLightbox(devolverFoco = true): void {
    const dialogo = this.lightbox?.nativeElement;
    this.restaurarFocoLightbox = devolverFoco;
    if (dialogo?.open) dialogo.close();
    else {
      this.lightboxAbierto.set(false);
      if (devolverFoco) queueMicrotask(() => this.botonImagenPrincipal?.nativeElement.focus());
      this.restaurarFocoLightbox = true;
    }
  }
  cerrarLightboxDesdeFondo(evento: MouseEvent): void {
    if (evento.target === this.lightbox?.nativeElement) this.cerrarLightbox(true);
  }
  alCerrarLightbox(): void {
    const devolverFoco = this.restaurarFocoLightbox;
    this.restaurarFocoLightbox = true; this.lightboxAbierto.set(false);
    if (devolverFoco) queueMicrotask(() => this.botonImagenPrincipal?.nativeElement.focus());
  }
  imagenCloudinary(url: string | null | undefined, width = 800): string {
    return cloudinaryResponsiveUrl(url, width);
  }
  srcsetCloudinary(url: string | null | undefined): string | null {
    return cloudinaryResponsiveSrcset(url);
  }

  imagenValida(url?: string): boolean { return Boolean(url && !this.imagenesFallidas().has(url)); }
  errorImagen(url: string): void {
    this.imagenesFallidas.update(actual => new Set([...actual, url]));
    if (url === this.imagenActual() && this.lightboxAbierto()) this.cerrarLightbox(false);
  }
  stockBajo(): boolean { return this.modeloSeleccionado()?.estadoDisponibilidad === 'lowStock'; }
  textoDisponibilidad(): string {
    const modelo = this.modeloSeleccionado();
    if (!modelo) return 'Agotado';
    const estado = etiquetaDisponibilidad(modelo);
    if (estado === 'Agotado') return estado;
    return `${estado} · ${modelo.stock} ${modelo.stock === 1 ? 'unidad' : 'unidades'}`;
  }
  precioModelo(modelo: ModeloTienda): number {
    const producto = this.producto();
    return producto ? precioVenta(producto, modelo) : modelo.precio;
  }
  rutaProducto(producto: ProductoTienda): string { return producto.slug ? STOREFRONT_PATHS.producto(producto.slug) : STOREFRONT_PATHS.productos; }
  rutaCategoria(): string {
    const categoria = this.categoriaProducto();
    return categoria ? STOREFRONT_PATHS.categoria(categoria.slug) : STOREFRONT_PATHS.categorias;
  }
  imagenRelacionado(producto: ProductoTienda): string {
    const modelo = producto.modelos.find(item => item.disponible) || producto.modelos[0];
    return modelo?.imagenes[0] || producto.imagenes[0] || '';
  }
  precioRelacionado(producto: ProductoTienda): number {
    const modelo = producto.modelos.find(item => item.disponible) || producto.modelos[0];
    return modelo ? precioVenta(producto, modelo) : producto.precio;
  }
  private enlaceProductoActual(): string {
    const view = this.document.defaultView;
    const producto = this.producto();
    if (!view || !producto?.slug) return '';
    try { return new URL(STOREFRONT_PATHS.producto(producto.slug), view.location.origin).toString(); }
    catch { return ''; }
  }

  moneda(valor: number): string {
    try { return new Intl.NumberFormat('es-HN', { style: 'currency', currency: this.identidad.config().moneda || 'HNL' }).format(valor); }
    catch { return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor); }
  }

  private cargarProducto(): void {
    this.cargaProducto?.unsubscribe();
    this.cargaRelacionados?.unsubscribe();
    this.catalogoContexto.set([]);
    this.producto.set(null); this.error.set(''); this.vistaWhatsapp.set(''); this.estado.set('loading');
    this.modeloClave.set(''); this.cantidad.set(0); this.imagenActiva.set(0); this.cerrarLightbox(false);
    const slug = this.slugSolicitado();
    if (!slug) {
      this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
      this.estado.set('not-found');
      return;
    }
    if (!this.utilizarDatosBaseDatos()) {
      const producto = crearCatalogoEjemplo().find(item => item.slug === slug && item.activo) || null;
      if (!producto) {
        this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
        this.estado.set('not-found');
        return;
      }
      this.establecerProducto(producto, slug); return;
    }
    this.cargaProducto = this.servicio.obtenerProductoPorSlug(slug).pipe(map(mapearProducto), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: producto => this.establecerProducto(producto, slug),
      error: error => {
        if (this.esNoEncontrado(error)) {
          this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
          this.estado.set('not-found');
          return;
        }
        this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
        this.error.set('No pudimos cargar este producto. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.estado.set('error');
      }
    });
  }
  private establecerProducto(producto: ProductoTienda, slugSolicitado: string): void {
    if (!producto.activo) {
      this.seo.aplicarNoIndex(this.identidad.config().nombreComercial || 'Tienda');
      this.estado.set('not-found');
      return;
    }
    this.producto.set(producto);
    this.sincronizarFavorito();
    this.cargarRelacionados(producto);
    const modelo = producto.modelos.find(item => item.disponible) || producto.modelos[0];
    this.modeloClave.set(modelo?.clave || '');
    this.estado.set('success');
    this.reiniciarCantidad();
    this.seo.aplicarProducto(
      producto,
      this.identidad.config().nombreComercial || 'Tienda',
      this.imagenes()[0],
      this.precioActual(),
      this.modeloSeleccionado()?.disponible,
      this.identidad.config().moneda || 'HNL'
    );
    if (producto.slug && producto.slug !== slugSolicitado) {
      const extras = this.retornoCatalogo
        ? { replaceUrl: true, state: { storefrontReturn: this.retornoCatalogo } }
        : { replaceUrl: true };
      void this.router.navigateByUrl(STOREFRONT_PATHS.producto(producto.slug), extras);
    }
  }
  private sincronizarFavorito(): void {
    const producto = this.producto();
    if (!producto || !this.cuentaCliente.autenticado() || !this.utilizarDatosBaseDatos()) {
      this.favorito.set(false);
      return;
    }
    this.cuentaCliente.favoritos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ids => this.favorito.set(ids.includes(producto.id)),
      error: () => this.favorito.set(false)
    });
  }

  private cargarContextoCatalogo(): void {
    this.cargaCatalogo?.unsubscribe();
    this.carritoStore.reiniciarContexto();
    const idsPersistidos = this.carritoStore.productoIdsPersistidos(
      this.identidad.config().id,
      this.utilizarDatosBaseDatos()
    );
    const fuente: Observable<ProductoCatalogoPublico[] | null> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerProductosContexto(idsPersistidos)
      : of(null);
    this.cargaCatalogo = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: datos => {
        const productos = datos === null ? crearCatalogoEjemplo() : datos.map(mapearProducto);
        const resultado = this.carritoStore.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
        if (resultado.ajustado) this.aviso.set(this.carritoStore.aviso());
        this.reiniciarCantidad();
      },
      error: () => this.aviso.set('El producto puede consultarse, pero no pudimos validar el carrito. Tu selección guardada no fue reemplazada.')
    });
  }

  private cargarRelacionados(productoActual: ProductoTienda): void {
    this.cargaRelacionados?.unsubscribe();
    this.catalogoContexto.set([]);

    if (!this.utilizarDatosBaseDatos()) {
      this.catalogoContexto.set(crearCatalogoEjemplo());
      return;
    }
    if (productoActual.categoriaId === null) return;

    this.cargaRelacionados = this.servicio.obtenerProductos(1, 5, {
      categoriaId: productoActual.categoriaId,
      sortBy: 'Nombre',
      sortDirection: 'asc'
    }).pipe(
      map(res => {
        if (!res.success || !res.data || !Array.isArray(res.data.items)) {
          throw new Error('Respuesta de relacionados no válida.');
        }
        return res.data.items.map(mapearProductoResumen);
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: productos => this.catalogoContexto.set(productos),
      error: () => this.aviso.set('No pudimos actualizar productos relacionados; el producto actual sigue disponible.')
    });
  }
  private cargarCategorias(): void {
    this.cargaCategorias?.unsubscribe(); this.categorias.set([]);
    const fuente: Observable<CategoriaTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCategorias().pipe(map(categorias => categorias.map(mapearCategoriaTienda)))
      : of(crearCategoriasTiendaEjemplo());
    this.cargaCategorias = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categorias => this.categorias.set(categorias),
      error: () => this.aviso.set('No pudimos actualizar la navegación de categorías en esta página.')
    });
  }
  private reiniciarCantidad(): void {
    this.cantidad.set(this.stockRestante() > 0 && this.modeloSeleccionado()?.disponible ? 1 : 0);
  }
  private leerContextoRetorno(): ContextoRetornoCatalogo | null {
    const view = this.document.defaultView;
    const estadoNavegacion = view?.history.state && typeof view.history.state === 'object'
      ? view.history.state as Record<string, unknown>
      : null;
    let valor = estadoNavegacion?.['storefrontReturn'];
    if (!valor || typeof valor !== 'object') {
      try {
        const persistido = view?.sessionStorage.getItem(RETORNO_CATALOGO_STORAGE);
        valor = persistido ? JSON.parse(persistido) as unknown : null;
      } catch {
        valor = null;
      }
    }
    if (!valor || typeof valor !== 'object') return null;
    const estado = valor as Record<string, unknown>;
    const url = typeof estado['url'] === 'string' ? estado['url'] : '';
    const scrollY = typeof estado['scrollY'] === 'number' && Number.isFinite(estado['scrollY']) ? estado['scrollY'] : 0;
    const productoId = typeof estado['productoId'] === 'number' && Number.isSafeInteger(estado['productoId'])
      ? estado['productoId'] : 0;
    if (!url.startsWith('/tienda') || url.startsWith('//') || productoId <= 0) return null;

    const modelosActivos: Record<number, string> = {};
    const modelos = estado['modelosActivos'];
    if (modelos && typeof modelos === 'object') {
      for (const [id, clave] of Object.entries(modelos as Record<string, unknown>)) {
        const producto = Number(id);
        if (Number.isSafeInteger(producto) && producto > 0 && typeof clave === 'string' && clave.length <= 180) {
          modelosActivos[producto] = clave;
        }
      }
    }

    return {
      url,
      scrollY: Math.max(0, Math.round(scrollY)),
      productoId,
      modelosActivos,
      filtrosAbiertos: estado['filtrosAbiertos'] === true
    };
  }

  private referenciaStorefront(view: Window): boolean {
    if (!this.document.referrer) return false;
    try {
      const referente = new URL(this.document.referrer);
      return referente.origin === view.location.origin && referente.pathname.startsWith('/tienda');
    } catch {
      return false;
    }
  }

  private esNoEncontrado(error: unknown): boolean {
    if (error instanceof HttpErrorResponse) return error.status === 404;
    return error instanceof Error && ['Producto no encontrado.', 'Slug de producto no válido.'].includes(error.message);
  }
}
