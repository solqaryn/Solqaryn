import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { Almacen } from '../../core/models/almacen.model';
import { MovimientoInventario } from '../../core/models/movimiento-inventario.model';
import { Producto, ProductoVariante } from '../../core/models/producto.model';
import { UbicacionAlmacen } from '../../core/models/ubicacion-almacen.model';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { AlmacenService } from '../../services/almacen.service';
import { MovimientoInventarioQuery, MovimientoInventarioService } from '../../services/movimiento-inventario.service';
import { ProductoService } from '../../services/producto.service';
import { UbicacionAlmacenService } from '../../services/ubicacion-almacen.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';

interface OpcionFiltro {
  valor: string;
  etiqueta: string;
}

@Component({
  selector: 'app-movimientos-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    ProductoImagenComponent
  ],
  templateUrl: './movimientos-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './movimientos-list.component.scss'
})
export class MovimientosListComponent implements OnInit {
  readonly movimientos = signal<MovimientoInventario[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly productos = signal<Producto[]>([]);
  readonly variantes = signal<ProductoVariante[]>([]);
  readonly almacenes = signal<Almacen[]>([]);
  readonly ubicaciones = signal<UbicacionAlmacen[]>([]);
  readonly cargandoVariantes = signal(false);
  readonly cargandoUbicaciones = signal(false);

  readonly tipos: OpcionFiltro[] = [
    { valor: 'Entrada', etiqueta: 'Entrada' },
    { valor: 'Salida', etiqueta: 'Salida' },
    { valor: 'Reversion', etiqueta: 'Reversión' },
    { valor: 'Ajuste', etiqueta: 'Ajuste' }
  ];

  readonly causas: OpcionFiltro[] = [
    { valor: 'NoEspecificada', etiqueta: 'No especificada' },
    { valor: 'Compra', etiqueta: 'Compra' },
    { valor: 'Venta', etiqueta: 'Venta' },
    { valor: 'ConsumoAdministrativo', etiqueta: 'Consumo administrativo' },
    { valor: 'AjusteManual', etiqueta: 'Ajuste manual' },
    { valor: 'AnulacionCompra', etiqueta: 'Anulación de compra' },
    { valor: 'AnulacionVenta', etiqueta: 'Anulación de venta' },
    { valor: 'ReversionConsumo', etiqueta: 'Reversión de consumo' }
  ];

  readonly origenes: OpcionFiltro[] = [
    { valor: 'Compra', etiqueta: 'Compra' },
    { valor: 'Venta', etiqueta: 'Venta' },
    { valor: 'ConsumoInsumo', etiqueta: 'Consumo administrativo' },
    { valor: 'AjusteInventario', etiqueta: 'Ajuste de inventario' }
  ];

  productoId: number | null = null;
  productoVarianteId: number | null = null;
  almacenId: number | null = null;
  ubicacionAlmacenId: number | null = null;
  filtroTipo = '';
  filtroCausa = '';
  correlationId = '';
  origenTipo = '';
  origenId: number | null = null;
  desde = '';
  hasta = '';
  page = 1;
  pageSize = 25;

  private readonly navigationDefaults = {
    productoId: null,
    productoVarianteId: null,
    almacenId: null,
    ubicacionAlmacenId: null,
    filtroTipo: '',
    filtroCausa: '',
    correlationId: '',
    origenTipo: '',
    origenId: null,
    desde: '',
    hasta: '',
    page: 1,
    pageSize: 25
  };

  constructor(
    private readonly movimientoService: MovimientoInventarioService,
    private readonly productoService: ProductoService,
    private readonly almacenService: AlmacenService,
    private readonly ubicacionService: UbicacionAlmacenService,
    private readonly route: ActivatedRoute,
    private readonly navigationState: ListNavigationStateService
  ) {}

  ngOnInit(): void {
    const state = this.navigationState.restore('inventario-movimientos', this.route, this.navigationDefaults);
    this.productoId = this.normalizarId(state.productoId);
    this.productoVarianteId = this.normalizarId(state.productoVarianteId);
    this.almacenId = this.normalizarId(state.almacenId);
    this.ubicacionAlmacenId = this.normalizarId(state.ubicacionAlmacenId);
    this.filtroTipo = this.tipos.some(x => x.valor === state.filtroTipo) ? state.filtroTipo : '';
    this.filtroCausa = this.causas.some(x => x.valor === state.filtroCausa) ? state.filtroCausa : '';
    this.correlationId = String(state.correlationId ?? '').trim();
    this.origenTipo = this.origenes.some(x => x.valor === state.origenTipo) ? state.origenTipo : '';
    this.origenId = this.normalizarId(state.origenId);
    this.desde = String(state.desde ?? '');
    this.hasta = String(state.hasta ?? '');
    this.page = Math.max(1, Math.trunc(Number(state.page) || 1));
    this.pageSize = [10, 25, 50, 100].includes(Number(state.pageSize)) ? Number(state.pageSize) : 25;

    forkJoin({
      productos: this.productoService.getPaged({ page: 1, pageSize: 100, activo: true, sortBy: 'nombre', sortDirection: 'asc' }),
      almacenes: this.almacenService.getActivos()
    }).subscribe({
      next: ({ productos, almacenes }) => {
        this.productos.set(productos.data?.items ?? []);
        this.almacenes.set(almacenes.data ?? []);
        if (this.productoId) this.cargarVariantes(this.productoId);
        if (this.almacenId) this.cargarUbicaciones(this.almacenId);
        this.cargar();
      },
      error: err => {
        this.loading.set(false);
        this.error.set(this.extraerError(err, 'No fue posible cargar los catálogos para consultar el Kardex.'));
      }
    });
  }

  cargar(): void {
    this.error.set('');
    this.loading.set(true);
    this.movimientoService.getPaged(this.construirQuery())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.movimientos.set([]);
            this.totalCount.set(0);
            this.error.set(response.message || 'No fue posible consultar el Kardex.');
            return;
          }

          this.movimientos.set(response.data.items ?? []);
          this.totalCount.set(response.data.totalCount ?? 0);
          this.page = response.data.page || this.page;
          this.pageSize = response.data.pageSize || this.pageSize;
          this.persistirEstado();
        },
        error: err => {
          this.movimientos.set([]);
          this.totalCount.set(0);
          this.error.set(this.extraerError(err, 'No fue posible consultar el Kardex.'));
        }
      });
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.persistirEstado();
    this.cargar();
  }

  limpiarFiltros(): void {
    this.navigationState.clear('inventario-movimientos');
    this.productoId = null;
    this.productoVarianteId = null;
    this.almacenId = null;
    this.ubicacionAlmacenId = null;
    this.filtroTipo = '';
    this.filtroCausa = '';
    this.correlationId = '';
    this.origenTipo = '';
    this.origenId = null;
    this.desde = '';
    this.hasta = '';
    this.page = 1;
    this.pageSize = 25;
    this.variantes.set([]);
    this.ubicaciones.set([]);
    this.cargar();
  }

  onProductoChange(): void {
    this.productoVarianteId = null;
    this.variantes.set([]);
    if (this.productoId) this.cargarVariantes(this.productoId);
  }

  onAlmacenChange(): void {
    this.ubicacionAlmacenId = null;
    this.ubicaciones.set([]);
    if (this.almacenId) this.cargarUbicaciones(this.almacenId);
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.persistirEstado();
    this.cargar();
  }

  etiquetaVariante(variante: ProductoVariante): string {
    return variante.etiqueta?.trim() || variante.sku || `Variante #${variante.id}`;
  }

  etiquetaCausa(causa: string): string {
    return this.causas.find(x => x.valor === causa)?.etiqueta ?? causa;
  }

  etiquetaOrigen(movimiento: MovimientoInventario): string {
    const tipo = movimiento.origenTipo || movimiento.referenciaTipo || 'Sin origen';
    const id = movimiento.origenId ?? movimiento.referenciaId;
    return id ? `${tipo} #${id}` : tipo;
  }

  etiquetaAlmacen(id?: number | null): string {
    if (!id) return 'Sin contexto físico';
    const almacen = this.almacenes().find(x => x.id === id);
    return almacen ? `${almacen.codigo} · ${almacen.nombre}` : `Almacén #${id}`;
  }

  etiquetaUbicacion(id?: number | null): string {
    if (!id) return 'Raíz / no especificada';
    const ubicacion = this.ubicaciones().find(x => x.id === id);
    return ubicacion ? `${ubicacion.codigo} · ${ubicacion.nombre}` : `Ubicación #${id}`;
  }

  private construirQuery(): MovimientoInventarioQuery {
    return {
      page: this.page,
      pageSize: this.pageSize,
      productoId: this.productoId ?? undefined,
      productoVarianteId: this.productoVarianteId ?? undefined,
      almacenId: this.almacenId ?? undefined,
      ubicacionAlmacenId: this.ubicacionAlmacenId ?? undefined,
      tipo: this.filtroTipo || undefined,
      causa: this.filtroCausa || undefined,
      correlationId: this.correlationId || undefined,
      origenTipo: this.origenTipo || undefined,
      origenId: this.origenId ?? undefined,
      desde: this.desde || undefined,
      hasta: this.hasta || undefined
    };
  }

  private cargarVariantes(productoId: number): void {
    this.cargandoVariantes.set(true);
    this.productoService.getVariantes(productoId, false)
      .pipe(finalize(() => this.cargandoVariantes.set(false)))
      .subscribe({
        next: response => this.variantes.set((response.data ?? []).filter(v => v.activo)),
        error: err => this.error.set(this.extraerError(err, 'No fue posible cargar las variantes del producto.'))
      });
  }

  private cargarUbicaciones(almacenId: number): void {
    this.cargandoUbicaciones.set(true);
    this.ubicacionService.getActivas(almacenId)
      .pipe(finalize(() => this.cargandoUbicaciones.set(false)))
      .subscribe({
        next: response => this.ubicaciones.set(response.data ?? []),
        error: err => this.error.set(this.extraerError(err, 'No fue posible cargar las ubicaciones del almacén.'))
      });
  }

  private persistirEstado(): void {
    this.navigationState.persist('inventario-movimientos', this.route, {
      productoId: this.productoId,
      productoVarianteId: this.productoVarianteId,
      almacenId: this.almacenId,
      ubicacionAlmacenId: this.ubicacionAlmacenId,
      filtroTipo: this.filtroTipo,
      filtroCausa: this.filtroCausa,
      correlationId: this.correlationId,
      origenTipo: this.origenTipo,
      origenId: this.origenId,
      desde: this.desde,
      hasta: this.hasta,
      page: this.page,
      pageSize: this.pageSize
    }, this.navigationDefaults);
  }

  private normalizarId(value: unknown): number | null {
    if (value === null || value === undefined || value === '') return null;
    const numero = Number(value);
    return Number.isInteger(numero) && numero > 0 ? numero : null;
  }

  private extraerError(err: any, fallback: string): string {
    return err?.error?.message || err?.error?.title || err?.message || fallback;
  }
}
