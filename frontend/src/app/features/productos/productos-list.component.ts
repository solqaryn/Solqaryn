import { Component, OnDestroy, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { debounceTime, forkJoin, Subject, Subscription } from 'rxjs';
import { ProductoService } from '../../services/producto.service';
import { CategoriaService } from '../../services/categoria.service';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { Producto } from '../../core/models/producto.model';
import { Categoria } from '../../core/models/categoria.model';
import { CatalogoProducto } from '../../core/models/catalogo-producto.model';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';

type EstadoProductoFiltro = 'todos' | 'activos' | 'inactivos' | 'agotados' | 'disponibles';

@Component({
  selector: 'app-productos-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatPaginatorModule,
    MatProgressSpinnerModule, MatDialogModule, MatSlideToggleModule, ProductoImagenComponent
  ],
  templateUrl: './productos-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './productos-list.component.scss'
})
export class ProductosListComponent implements OnInit, OnDestroy {
  readonly productos = signal<Producto[]>([]);
  readonly categorias = signal<Categoria[]>([]);
  readonly colores = signal<CatalogoProducto[]>([]);
  readonly tallas = signal<CatalogoProducto[]>([]);
  readonly marcas = signal<CatalogoProducto[]>([]);
  readonly modelos = signal<CatalogoProducto[]>([]);
  readonly loading = signal(true);
  readonly loadingFilters = signal(true);
  readonly totalCount = signal(0);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  page = 1;
  pageSize = 10;
  search = '';
  sortBy = 'Nombre';
  sortDirection: 'asc' | 'desc' = 'asc';
  categoriaId: number | null = null;
  colorId: number | null = null;
  tallaId: number | null = null;
  marcaId: number | null = null;
  modeloId: number | null = null;
  estado: EstadoProductoFiltro = 'todos';

  private readonly navigationDefaults = {
    page: 1,
    pageSize: 10,
    search: '',
    sortBy: 'Nombre',
    sortDirection: 'asc',
    categoriaId: 0,
    colorId: 0,
    tallaId: 0,
    marcaId: 0,
    modeloId: 0,
    estado: 'todos'
  };
  private readonly searchSubject = new Subject<string>();
  private readonly searchSubscription: Subscription;
  private cargaActual?: Subscription;
  private secuenciaCarga = 0;

  constructor(
    private productoService: ProductoService,
    private categoriaService: CategoriaService,
    private catalogoService: CatalogoProductoService,
    private dialog: MatDialog,
    private permisosRuntime: PermisosRuntimeService,
    private route: ActivatedRoute,
    private navigationState: ListNavigationStateService
  ) {
    this.searchSubscription = this.searchSubject.pipe(debounceTime(350)).subscribe(() => {
      this.page = 1;
      this.cargar();
    });
  }

  ngOnInit(): void {
    this.restaurarEstado();
    this.puedeCrear.set(this.permisosRuntime.puede('Productos', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Productos', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Productos', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Productos', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Productos', 'EliminarLogico'));
    this.cargarCatalogosFiltro();
    this.cargar();
  }

  ngOnDestroy(): void {
    this.secuenciaCarga++;
    this.cargaActual?.unsubscribe();
    this.searchSubscription.unsubscribe();
    this.searchSubject.complete();
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.searchSubject.next(value);
  }

  onMarcaFilterChange(marcaId: number | null): void {
    this.marcaId = marcaId;
    this.modeloId = null;
    this.modelos.set([]);

    if (!marcaId) {
      this.aplicarFiltros();
      return;
    }

    this.catalogoService.getAll('Modelo', '', marcaId).subscribe({
      next: (res) => {
        this.modelos.set(res.data);
        this.aplicarFiltros();
      },
      error: () => this.aplicarFiltros()
    });
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.navigationState.clear('productos');
    this.page = 1;
    this.pageSize = 10;
    this.search = '';
    this.sortBy = 'Nombre';
    this.sortDirection = 'asc';
    this.categoriaId = null;
    this.colorId = null;
    this.tallaId = null;
    this.marcaId = null;
    this.modeloId = null;
    this.estado = 'todos';
    this.modelos.set([]);
    this.cargar();
  }

  cargar(): void {
    this.persistirEstado();
    this.loading.set(true);

    const cargaId = ++this.secuenciaCarga;
    this.cargaActual?.unsubscribe();
    this.cargaActual = this.productoService.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      search: this.search,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
      categoriaId: this.categoriaId ?? undefined,
      colorId: this.colorId ?? undefined,
      tallaId: this.tallaId ?? undefined,
      marcaId: this.marcaId ?? undefined,
      modeloId: this.modeloId ?? undefined,
      activo: this.estado === 'activos' ? true : this.estado === 'inactivos' ? false : undefined,
      agotado: this.estado === 'agotados' ? true : this.estado === 'disponibles' ? false : undefined
    }).subscribe({
      next: (res) => {
        if (cargaId !== this.secuenciaCarga) return;
        this.productos.set(res.data.items);
        this.totalCount.set(res.data.totalCount);
        this.loading.set(false);
      },
      error: () => {
        if (cargaId === this.secuenciaCarga) this.loading.set(false);
      }
    });
  }

  puedeCambiarEstado(producto: Producto): boolean {
    return producto.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  cambiarEstado(producto: Producto): void {
    if (!this.puedeCambiarEstado(producto)) return;

    const operacion = producto.activo
      ? this.productoService.desactivar(producto.id)
      : this.productoService.activar(producto.id);

    operacion.subscribe({
      next: () => this.cargar(),
      error: () => this.cargar()
    });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }

  ordenarPor(campo: string): void {
    if (this.sortBy === campo) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = campo;
      this.sortDirection = 'asc';
    }
    this.page = 1;
    this.cargar();
  }

  eliminar(producto: Producto): void {
    if (!this.puedeEliminar()) return;

    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar producto',
        message: `¿Deseas eliminar lógicamente "${producto.nombre}"? Su historial e imágenes se conservarán.`
      }
    });

    ref.afterClosed().subscribe((confirmado) => {
      if (!confirmado) return;
      this.productoService.delete(producto.id).subscribe(() => this.cargar());
    });
  }

  private restaurarEstado(): void {
    const state = this.navigationState.restore('productos', this.route, this.navigationDefaults);
    this.page = this.positiveInt(state.page, 1);
    this.pageSize = [10, 25, 50].includes(state.pageSize) ? state.pageSize : 10;
    this.search = state.search;
    this.sortBy = ['Nombre', 'Marca', 'Cantidad', 'Costo', 'Precio'].includes(state.sortBy) ? state.sortBy : 'Nombre';
    this.sortDirection = state.sortDirection === 'desc' ? 'desc' : 'asc';
    this.categoriaId = this.optionalId(state.categoriaId);
    this.colorId = this.optionalId(state.colorId);
    this.tallaId = this.optionalId(state.tallaId);
    this.marcaId = this.optionalId(state.marcaId);
    this.modeloId = this.optionalId(state.modeloId);
    this.estado = ['todos', 'activos', 'inactivos', 'agotados', 'disponibles'].includes(state.estado)
      ? state.estado as EstadoProductoFiltro
      : 'todos';
  }

  private persistirEstado(): void {
    this.navigationState.persist('productos', this.route, {
      page: this.page,
      pageSize: this.pageSize,
      search: this.search,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
      categoriaId: this.categoriaId ?? 0,
      colorId: this.colorId ?? 0,
      tallaId: this.tallaId ?? 0,
      marcaId: this.marcaId ?? 0,
      modeloId: this.modeloId ?? 0,
      estado: this.estado
    }, this.navigationDefaults);
  }

  private positiveInt(value: number, fallback: number): number {
    const normalized = Math.trunc(value);
    return normalized > 0 ? normalized : fallback;
  }

  private optionalId(value: number): number | null {
    const normalized = Math.trunc(value);
    return normalized > 0 ? normalized : null;
  }

  private cargarCatalogosFiltro(): void {
    this.loadingFilters.set(true);
    forkJoin({
      categorias: this.categoriaService.getAll(),
      colores: this.catalogoService.getAll('Color'),
      tallas: this.catalogoService.getAll('Talla'),
      marcas: this.catalogoService.getAll('Marca')
    }).subscribe({
      next: (res) => {
        this.categorias.set(res.categorias.data);
        this.colores.set(res.colores.data);
        this.tallas.set(res.tallas.data);
        this.marcas.set(res.marcas.data);
        this.loadingFilters.set(false);
        if (this.marcaId) {
          this.catalogoService.getAll('Modelo', '', this.marcaId).subscribe({
            next: (modelos) => this.modelos.set(modelos.data),
            error: () => this.modelos.set([])
          });
        }
      },
      error: () => this.loadingFilters.set(false)
    });
  }
}
