import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { VaristorehnIdentidadService } from './varistorehn-identidad.service';
import {
  CategoriaTienda,
  EstadoConsultaPublica,
  ModeloTienda,
  OrdenCatalogo,
  ProductoTienda,
  crearCatalogoEjemplo,
  etiquetaDisponibilidad,
  filtrarProductos,
  mapearProducto,
  mapearProductoResumen,
  precioVenta
} from './varistorehn.catalog';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './varistorehn-categorias.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { VARISTOREHN_CONFIG } from './varistorehn.config';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { VaristorehnService } from './varistorehn.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

interface ContextoRetornoCatalogo {
  url: string;
  scrollY: number;
  productoId: number;
  modelosActivos: Record<number, string>;
  filtrosAbiertos: boolean;
}

const RETORNO_CATALOGO_STORAGE = 'varistorehn:retorno-catalogo:v1';

@Component({
  selector: 'app-varistorehn-productos',
  standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn-productos.component.html',
  styleUrl: './varistorehn-productos.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnProductosComponent implements OnInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly identidad = inject(VaristorehnIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);
  readonly carritoStore = inject(VaristorehnCarritoService);

  readonly controlesVistaPrevia = !environment.production && this.config.mostrarControlesVistaPrevia;
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly permiteWhatsapp = this.config.modoCarrito !== 'tarjeta';

  readonly productos = signal<ProductoTienda[]>([]);
  readonly totalResultados = signal(0);
  readonly cargando = signal(true);
  readonly errorCatalogo = signal('');
  readonly estadoCatalogo = computed<EstadoConsultaPublica>(() => {
    if (this.cargando()) return 'loading';
    if (this.errorCatalogo()) return 'error';
    if (this.productos().length || this.totalResultados() > 0 || this.filtrosActivos()) return 'success';
    return 'empty';
  });

  readonly categorias = signal<CategoriaTienda[]>([]);
  readonly cargandoCategorias = signal(true);
  readonly errorCategorias = signal('');
  readonly categoriasNavegacion = computed(() => this.categorias().map(categoria => categoria.nombre));

  readonly busqueda = signal('');
  readonly categoriaSlug = signal('');
  readonly categoriaActiva = signal('');
  readonly soloDisponibles = signal(false);
  readonly soloOfertas = signal(false);
  readonly soloOfertasPagina = signal(false);
  readonly precioMinimo = signal<number | null>(null);
  readonly precioMaximo = signal<number | null>(null);
  readonly orden = signal<OrdenCatalogo>('relevancia');
  readonly pagina = signal(1);
  readonly tamanoPagina = 12;
  readonly filtrosAbiertos = signal(false);
  readonly modelosActivos = signal<Record<number, string>>(this.modelosRetornoInicial());
  readonly imagenesFallidas = signal<Set<string>>(new Set());

  readonly carrito = this.carritoStore.items;
  readonly totalUnidades = computed<number | null>(() => this.carritoStore.listo() ? this.carritoStore.totalUnidades() : null);
  readonly subtotal = computed<number | null>(() => this.carritoStore.listo() ? this.carritoStore.subtotal() : null);
  readonly aviso = signal('');

  readonly resultados = computed(() => filtrarProductos(this.productos(), {
    busqueda: this.busqueda(),
    categoria: this.categoriaActiva(),
    soloDisponibles: this.soloDisponibles(),
    soloOfertas: this.soloOfertas() || this.soloOfertasPagina(),
    precioMinimo: this.precioMinimo(),
    precioMaximo: this.precioMaximo(),
    orden: this.orden()
  }));
  readonly totalResultadosMostrados = computed(() => {
    const locales = this.resultados().length;
    return locales < this.productos().length ? locales : this.totalResultados();
  });
  readonly totalPaginas = computed(() => Math.max(1, Math.ceil(this.totalResultados() / this.tamanoPagina)));
  readonly productosVisibles = computed(() => this.resultados());
  readonly hayFiltros = computed(() => Boolean(
    this.busqueda().trim() || this.categoriaSlug() || this.soloDisponibles() || this.soloOfertas()
      || this.precioMinimo() !== null || this.precioMaximo() !== null || this.orden() !== 'relevancia'
  ));
  readonly categoriaFiltroInvalida = computed(() => Boolean(
    !this.cargandoCategorias() && !this.errorCategorias() && this.categoriaSlug() && !this.categoriaActiva()
  ));

  private filtrosActivos(): boolean {
    return Boolean(
      this.busqueda().trim()
      || this.categoriaSlug()
      || this.soloDisponibles()
      || this.soloOfertas()
      || this.soloOfertasPagina()
      || this.precioMinimo() !== null
      || this.precioMaximo() !== null
      || this.orden() !== 'relevancia'
    );
  }

  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    productos: VARISTOREHN_PATHS.productos,
    ofertas: VARISTOREHN_PATHS.ofertas,
    categorias: VARISTOREHN_PATHS.categorias,
    contacto: `${VARISTOREHN_PATHS.inicio}#contacto`
  } as const;

  private cargaCatalogo?: Subscription;
  private cargaCategorias?: Subscription;
  private cargaCarrito?: Subscription;
  private scrollRetorno: number | null = null;
  private scrollRetornoRestaurado = false;

  ngOnInit(): void {
    this.hidratarEstadoRetorno();
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      this.soloOfertasPagina.set(data['soloOfertas'] === true);
    });
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      this.busqueda.set((params.get('q') || '').trim().slice(0, 180));
      this.categoriaSlug.set((params.get('categoria') || '').trim().slice(0, 180));
      this.soloDisponibles.set(params.get('disponible') === '1');
      this.soloOfertas.set(params.get('oferta') === '1');
      this.precioMinimo.set(this.numeroQuery(params.get('precioMin')));
      this.precioMaximo.set(this.numeroQuery(params.get('precioMax')));
      const ordenQuery = params.get('orden');
      this.orden.set(this.ordenQuery(ordenQuery));
      this.pagina.set(this.paginaQuery(params.get('pagina')));
      this.aplicarCategoriaDesdeSlug();
      if (ordenQuery && ordenQuery !== this.orden()) queueMicrotask(() => this.sincronizarUrl());
      this.normalizarPagina();
      if (!this.cargandoCategorias()) this.cargarCatalogo();
    });
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.recargar());
  }

  recargar(): void {
    this.cargarCategoriasPublicas();
    this.cargarContextoCarrito();
  }

  cambiarFuente(baseDatos: boolean): void {
    if (!this.controlesVistaPrevia || baseDatos === this.utilizarDatosBaseDatos()) return;
    this.utilizarDatosBaseDatos.set(baseDatos);
    this.modelosActivos.set({});
    this.carritoStore.reiniciarContexto();
    this.recargar();
  }

  actualizarBusqueda(texto: string): void { this.busqueda.set(texto.slice(0, 180)); this.pagina.set(1); }
  buscar(): void { this.pagina.set(1); this.sincronizarUrl(); this.irInicioCatalogo(); }

  seleccionarCategoria(nombre: string): void {
    if (!nombre) {
      this.categoriaActiva.set('');
      this.categoriaSlug.set('');
      this.pagina.set(1);
      this.sincronizarUrl();
      return;
    }
    const categoria = this.categorias().find(item => item.nombre === nombre);
    if (!categoria) { this.aviso.set('La categoría seleccionada ya no está disponible.'); return; }
    this.seleccionarCategoriaPorSlug(categoria.slug);
  }

  seleccionarCategoriaPorSlug(slug: string): void {
    const categoria = this.categorias().find(item => item.slug === slug);
    if (!categoria) return;
    this.categoriaSlug.set(categoria.slug);
    this.categoriaActiva.set(categoria.nombre);
    this.pagina.set(1);
    this.sincronizarUrl();
  }

  cambiarDisponibilidad(valor: boolean): void {
    this.soloDisponibles.set(valor);
    this.pagina.set(1);
    this.sincronizarUrl();
  }
  cambiarOfertas(valor: boolean): void {
    this.soloOfertas.set(valor);
    this.pagina.set(1);
    this.sincronizarUrl();
  }
  cambiarPrecioMinimo(valor: string): void {
    this.precioMinimo.set(this.numeroQuery(valor));
    this.pagina.set(1);
    this.sincronizarUrl();
  }
  cambiarPrecio(valor: string): void {
    this.precioMaximo.set(this.numeroQuery(valor));
    this.pagina.set(1);
    this.sincronizarUrl();
  }
  cambiarOrden(valor: string): void {
    this.orden.set(this.ordenQuery(valor));
    this.pagina.set(1);
    this.sincronizarUrl();
  }
  limpiarFiltros(): void {
    this.busqueda.set(''); this.categoriaSlug.set(''); this.categoriaActiva.set('');
    this.soloDisponibles.set(false); this.soloOfertas.set(false); this.precioMinimo.set(null); this.precioMaximo.set(null);
    this.orden.set('relevancia'); this.pagina.set(1);
    this.sincronizarUrl();
  }
  cambiarPagina(cambio: number): void {
    this.pagina.set(Math.max(1, Math.min(this.totalPaginas(), this.pagina() + cambio)));
    this.sincronizarUrl();
    this.irInicioCatalogo();
  }

  abrirCarrito(): void { void this.router.navigateByUrl(VARISTOREHN_PATHS.carrito); }

  abrirProducto(evento: MouseEvent, producto: ProductoTienda): void {
    if (!producto.slug || evento.defaultPrevented || evento.button !== 0
      || evento.ctrlKey || evento.metaKey || evento.shiftKey || evento.altKey) return;
    const view = this.document.defaultView;
    if (!view) return;

    const url = `${view.location.pathname}${view.location.search}${view.location.hash}`;
    const contexto: ContextoRetornoCatalogo = {
      url,
      scrollY: Math.max(0, Math.round(view.scrollY)),
      productoId: producto.id,
      modelosActivos: { ...this.modelosActivos() },
      filtrosAbiertos: this.filtrosAbiertos()
    };
    const estadoActual = view.history.state && typeof view.history.state === 'object'
      ? view.history.state as Record<string, unknown>
      : {};
    view.history.replaceState({ ...estadoActual, varistorehnCatalogState: contexto }, '', url);
    try {
      view.sessionStorage.setItem(RETORNO_CATALOGO_STORAGE, JSON.stringify(contexto));
    } catch {
      // History state remains the primary mechanism when storage is unavailable.
    }

    // Keep native anchor navigation so browser history/BFCache can restore the exact visual state.
  }

  modeloSeleccionado(producto: ProductoTienda): ModeloTienda {
    return producto.modelos.find(modelo => modelo.clave === this.modelosActivos()[producto.id])
      || producto.modelos.find(modelo => modelo.disponible) || producto.modelos[0];
  }
  seleccionarModelo(producto: ProductoTienda, clave: string): void {
    if (!producto.modelos.some(modelo => modelo.clave === clave)) return;
    this.modelosActivos.update(actual => ({ ...actual, [producto.id]: clave }));
  }
  disponibleParaAgregar(producto: ProductoTienda): boolean {
    const modelo = this.modeloSeleccionado(producto);
    return this.carritoStore.disponibleParaAgregar(producto, modelo);
  }
  agregar(producto: ProductoTienda): void {
    const modelo = this.modeloSeleccionado(producto);
    if (!this.carritoStore.disponibleParaAgregar(producto, modelo)) return;
    const agregadas = this.carritoStore.agregar(producto, modelo, 1);
    if (agregadas) this.aviso.set(`${producto.nombre} se agregó al carrito.`);
  }

  imagenValida(url?: string): boolean { return Boolean(url && !this.imagenesFallidas().has(url)); }
  errorImagen(url: string): void { this.imagenesFallidas.update(actual => new Set([...actual, url])); }
  stockBajo(modelo: ModeloTienda): boolean { return modelo.estadoDisponibilidad === 'lowStock'; }
  textoDisponibilidad(modelo: ModeloTienda): string { return etiquetaDisponibilidad(modelo); }
  precioActual(producto: ProductoTienda, modelo: ModeloTienda): number { return precioVenta(producto, modelo); }
  tieneOferta(modelo: ModeloTienda): boolean {
    return modelo.ofertaActiva && modelo.precioOferta !== null && modelo.precioOferta < modelo.precio;
  }
  ahorro(modelo: ModeloTienda): number { return Math.max(0, modelo.precio - (modelo.precioOferta ?? modelo.precio)); }
  porcentajeAhorro(modelo: ModeloTienda): number {
    return modelo.precio > 0 ? Math.round(this.ahorro(modelo) * 100 / modelo.precio) : 0;
  }

  moneda(valor: number): string {
    try { return new Intl.NumberFormat('es-HN', { style: 'currency', currency: this.identidad.config().moneda || 'HNL' }).format(valor); }
    catch { return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor); }
  }

  cargarCatalogo(): void {
    this.cargaCatalogo?.unsubscribe();
    this.cargando.set(true);
    this.errorCatalogo.set('');
    this.productos.set([]);
    this.totalResultados.set(0);

    const categoria = this.categorias().find(item => item.slug === this.categoriaSlug()) ?? null;
    const filtros = {
      busqueda: this.busqueda(),
      categoria: this.categoriaActiva(),
      soloDisponibles: this.soloDisponibles(),
      soloOfertas: this.soloOfertas() || this.soloOfertasPagina(),
      precioMinimo: this.precioMinimo(),
      precioMaximo: this.precioMaximo(),
      orden: this.orden()
    };

    const fuente: Observable<{ productos: ProductoTienda[]; total: number }> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerProductos(this.pagina(), this.tamanoPagina, {
          search: this.busqueda(),
          categoriaId: categoria?.id ?? null,
          soloDisponibles: this.soloDisponibles(),
          soloOfertas: this.soloOfertas() || this.soloOfertasPagina(),
          precioMinimo: this.precioMinimo(),
          precioMaximo: this.precioMaximo(),
          ...this.ordenServidor()
        }).pipe(map(res => {
          const datos = res.data;
          if (!res.success || !datos || !Array.isArray(datos.items)
            || !Number.isSafeInteger(datos.totalCount) || datos.totalCount < 0) {
            throw new Error('Respuesta de catálogo no válida.');
          }
          return { productos: datos.items.map(mapearProductoResumen), total: datos.totalCount };
        }))
      : of((() => {
          const todos = filtrarProductos(crearCatalogoEjemplo(), filtros);
          const inicio = (this.pagina() - 1) * this.tamanoPagina;
          return { productos: todos.slice(inicio, inicio + this.tamanoPagina), total: todos.length };
        })());

    this.cargaCatalogo = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: resultado => {
        this.productos.set(resultado.productos);
        this.totalResultados.set(resultado.total);
        this.cargando.set(false);
        this.normalizarPagina();
        this.intentarRestaurarScroll();
      },
      error: () => {
        this.errorCatalogo.set('No pudimos cargar el catálogo. Revisa la conexión e intenta de nuevo. No se sustituyeron los datos reales por ejemplos.');
        this.cargando.set(false);
      }
    });
  }

  private cargarCategoriasPublicas(): void {
    this.cargaCategorias?.unsubscribe();
    this.cargandoCategorias.set(true);
    this.errorCategorias.set('');
    this.categorias.set([]);
    this.categoriaActiva.set('');
    const fuente: Observable<CategoriaTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerCategorias().pipe(map(categorias => categorias.map(mapearCategoriaTienda)))
      : of(crearCategoriasTiendaEjemplo());
    this.cargaCategorias = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: categorias => {
        this.categorias.set(categorias);
        this.cargandoCategorias.set(false);
        this.aplicarCategoriaDesdeSlug();
        this.cargarCatalogo();
      },
      error: () => {
        this.errorCategorias.set('No pudimos cargar las categorías públicas. Los demás filtros siguen disponibles.');
        this.cargandoCategorias.set(false);
        this.cargarCatalogo();
      }
    });
  }

  private cargarContextoCarrito(): void {
    this.cargaCarrito?.unsubscribe();
    this.carritoStore.reiniciarContexto();
    const idsPersistidos = this.carritoStore.productoIdsPersistidos(
      this.identidad.config().id,
      this.utilizarDatosBaseDatos()
    );
    const fuente: Observable<ProductoTienda[]> = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerProductosContexto(idsPersistidos).pipe(map(items => items.map(mapearProducto)))
      : of(crearCatalogoEjemplo());

    this.cargaCarrito = fuente.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: productos => {
        const resultado = this.carritoStore.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
        if (resultado.ajustado) this.aviso.set(this.carritoStore.aviso());
      },
      error: () => this.aviso.set('No pudimos actualizar el carrito contra el catálogo vigente. Tu selección sigue guardada.')
    });
  }

  private ordenServidor(): { sortBy: 'Nombre' | 'Precio' | 'FechaCreacion'; sortDirection: 'asc' | 'desc' } {
    switch (this.orden()) {
      case 'precio-asc': return { sortBy: 'Precio', sortDirection: 'asc' };
      case 'precio-desc': return { sortBy: 'Precio', sortDirection: 'desc' };
      case 'recientes': return { sortBy: 'FechaCreacion', sortDirection: 'desc' };
      case 'nombre': return { sortBy: 'Nombre', sortDirection: 'asc' };
      default: return { sortBy: 'Nombre', sortDirection: 'asc' };
    }
  }

  private aplicarCategoriaDesdeSlug(): void {
    const slug = this.categoriaSlug();
    if (!slug) { this.categoriaActiva.set(''); return; }
    const categoria = this.categorias().find(item => item.slug === slug);
    this.categoriaActiva.set(categoria?.nombre || '');
  }

  private sincronizarUrl(): void {
    const q = this.busqueda().trim();
    const categoria = this.categoriaSlug();
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        q: q || null,
        categoria: categoria || null,
        disponible: this.soloDisponibles() ? '1' : null,
        oferta: this.soloOfertas() && !this.soloOfertasPagina() ? '1' : null,
        precioMin: this.precioMinimo(),
        precioMax: this.precioMaximo(),
        orden: this.orden() === 'relevancia' ? null : this.orden(),
        pagina: this.pagina() > 1 ? this.pagina() : null
      },
      replaceUrl: true
    });
  }

  private numeroQuery(valor: string | null): number | null {
    if (valor === null || !valor.trim()) return null;
    const numero = Number(valor);
    return Number.isFinite(numero) && numero >= 0 ? numero : null;
  }

  private paginaQuery(valor: string | null): number {
    const numero = Number(valor);
    return Number.isSafeInteger(numero) && numero > 0 ? numero : 1;
  }

  private ordenQuery(valor: string | null): OrdenCatalogo {
    if (valor === 'destacados') return 'relevancia';
    return ['relevancia', 'precio-asc', 'precio-desc', 'recientes', 'nombre'].includes(valor || '')
      ? valor as OrdenCatalogo
      : 'relevancia';
  }

  private normalizarPagina(): void {
    if (this.cargando() || this.cargandoCategorias() || this.errorCatalogo()) return;
    const paginaValida = Math.max(1, Math.min(this.pagina(), this.totalPaginas()));
    if (paginaValida === this.pagina()) return;
    this.pagina.set(paginaValida);
    this.sincronizarUrl();
  }

  private modelosRetornoInicial(): Record<number, string> {
    const view = this.document.defaultView;
    if (!view) return {};

    const urlActual = `${view.location.pathname}${view.location.search}${view.location.hash}`;
    const estadoNavegacion = view.history.state && typeof view.history.state === 'object'
      ? view.history.state as Record<string, unknown>
      : null;
    const desdeHistory = this.leerContextoRetorno(estadoNavegacion?.['varistorehnCatalogState']);
    if (desdeHistory?.url === urlActual) return desdeHistory.modelosActivos;

    try {
      const persistido = view.sessionStorage.getItem(RETORNO_CATALOGO_STORAGE);
      const contexto = persistido ? this.leerContextoRetorno(JSON.parse(persistido) as unknown) : null;
      return contexto?.url === urlActual ? contexto.modelosActivos : {};
    } catch {
      return {};
    }
  }

  private hidratarEstadoRetorno(): void {
    const view = this.document.defaultView;
    if (!view) return;
    const estadoNavegacion = view.history.state && typeof view.history.state === 'object'
      ? view.history.state as Record<string, unknown>
      : null;
    let contexto = this.leerContextoRetorno(estadoNavegacion?.['varistorehnCatalogState']);
    if (!contexto) {
      try {
        const persistido = view.sessionStorage.getItem(RETORNO_CATALOGO_STORAGE);
        contexto = persistido ? this.leerContextoRetorno(JSON.parse(persistido) as unknown) : null;
      } catch {
        contexto = null;
      }
    }
    if (!contexto) return;

    const urlActual = `${view.location.pathname}${view.location.search}${view.location.hash}`;
    if (contexto.url !== urlActual) return;

    this.modelosActivos.set(contexto.modelosActivos);
    this.filtrosAbiertos.set(contexto.filtrosAbiertos);
    this.scrollRetorno = contexto.scrollY;
    try { view.sessionStorage.removeItem(RETORNO_CATALOGO_STORAGE); } catch { /* no-op */ }
  }

  private leerContextoRetorno(valor: unknown): ContextoRetornoCatalogo | null {
    if (!valor || typeof valor !== 'object') return null;
    const estado = valor as Record<string, unknown>;
    const url = typeof estado['url'] === 'string' ? estado['url'] : '';
    const scrollY = typeof estado['scrollY'] === 'number' && Number.isFinite(estado['scrollY']) ? estado['scrollY'] : 0;
    const productoId = typeof estado['productoId'] === 'number' && Number.isSafeInteger(estado['productoId'])
      ? estado['productoId'] : 0;
    if (!url.startsWith('/varistorehn') || url.startsWith('//') || productoId <= 0) return null;

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

  private intentarRestaurarScroll(): void {
    if (this.scrollRetornoRestaurado || this.scrollRetorno === null || this.cargando() || this.cargandoCategorias()) return;
    const view = this.document.defaultView;
    if (!view) return;
    const destino = this.scrollRetorno;
    this.scrollRetornoRestaurado = true;
    view.requestAnimationFrame(() => view.requestAnimationFrame(() => view.scrollTo({ top: destino, left: 0, behavior: 'auto' })));
  }

  private irInicioCatalogo(): void {
    queueMicrotask(() => this.document.getElementById('catalogo-productos')?.scrollIntoView({ block: 'start' }));
  }
}
