import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Observable, Subscription, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { VaristorehnIdentidadService } from './varistorehn-identidad.service';
import { CategoriaTienda, ProductoCatalogoPublico, ProductoTienda, crearCatalogoEjemplo, mapearProducto } from './varistorehn.catalog';
import { VaristorehnCarritoService } from './varistorehn-carrito.service';
import { crearCategoriasTiendaEjemplo, mapearCategoriaTienda } from './varistorehn-categorias.catalog';
import { VaristorehnHeaderComponent } from './varistorehn-header.component';
import { VARISTOREHN_CONFIG } from './varistorehn.config';
import { EstadoConsultaPublica } from './varistorehn.models';
import { VARISTOREHN_PATHS } from './varistorehn.paths';
import { VaristorehnService } from './varistorehn.service';
import { IconoTiendaComponent, IlustracionTiendaComponent } from './varistorehn.visual';

@Component({
  selector: 'app-varistorehn-carrito',
  standalone: true,
  imports: [CommonModule, VaristorehnHeaderComponent, IconoTiendaComponent, IlustracionTiendaComponent],
  templateUrl: './varistorehn-carrito.component.html',
  styleUrl: './varistorehn-carrito.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VaristorehnCarritoComponent implements OnInit {
  private readonly servicio = inject(VaristorehnService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  readonly identidad = inject(VaristorehnIdentidadService);
  readonly config = inject(VARISTOREHN_CONFIG);
  readonly carrito = inject(VaristorehnCarritoService);

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
  /** Solo en vista previa de desarrollo propagamos explícitamente la fuente elegida entre rutas. */
  readonly enlaceCheckout = computed(() => this.controlesVistaPrevia && this.utilizarDatosBaseDatos()
    ? `${VARISTOREHN_PATHS.checkout}?fuente=bd`
    : VARISTOREHN_PATHS.checkout);

  readonly enlaces = {
    inicio: VARISTOREHN_PATHS.inicio,
    productos: VARISTOREHN_PATHS.productos,
    ofertas: VARISTOREHN_PATHS.ofertas,
    categorias: VARISTOREHN_PATHS.categorias,
    contacto: `${VARISTOREHN_PATHS.inicio}#contacto`
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
    void this.router.navigate(['/varistorehn/productos'], { queryParams: q ? { q } : {} });
  }

  seleccionarCategoria(nombre: string): void {
    if (!nombre) {
      void this.router.navigateByUrl(VARISTOREHN_PATHS.productos);
      return;
    }
    const categoria = this.categorias().find(item => item.nombre === nombre);
    if (!categoria) return;
    void this.router.navigateByUrl(VARISTOREHN_PATHS.categoria(categoria.slug));
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
