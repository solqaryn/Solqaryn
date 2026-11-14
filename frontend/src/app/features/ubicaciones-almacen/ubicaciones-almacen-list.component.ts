
import { Component, OnDestroy, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin, Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { Almacen } from '../../core/models/almacen.model';
import { TipoUbicacionAlmacenOpcion, UbicacionAlmacen } from '../../core/models/ubicacion-almacen.model';
import { AlmacenService } from '../../services/almacen.service';
import { UbicacionAlmacenService } from '../../services/ubicacion-almacen.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

type EstadoFiltro = 'todos' | 'activas' | 'inactivas';
type PadreFiltro = 'todos' | 'raiz' | number;

@Component({
  selector: 'app-ubicaciones-almacen-list',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule
],
  templateUrl: './ubicaciones-almacen-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './ubicaciones-almacen-list.component.scss'
})
export class UbicacionesAlmacenListComponent implements OnInit, OnDestroy {
  readonly ubicaciones = signal<UbicacionAlmacen[]>([]);
  readonly almacenes = signal<Almacen[]>([]);
  readonly padresDisponibles = signal<UbicacionAlmacen[]>([]);
  readonly tipos = signal<TipoUbicacionAlmacenOpcion[]>([]);
  readonly total = signal(0);
  readonly totalPaginas = signal(0);
  readonly loading = signal(true);
  readonly loadingCatalogos = signal(true);
  readonly loadingPadres = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly operandoIds = signal<Set<number>>(new Set<number>());

  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  buscar = '';
  estado: EstadoFiltro = 'todos';
  almacenId: number | null = null;
  padreFiltro: PadreFiltro = 'todos';
  tipo = '';
  pagina = 1;
  tamanoPagina = 10;

  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private ubicacionService: UbicacionAlmacenService,
    private almacenService: AlmacenService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('UbicacionesAlmacen', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('UbicacionesAlmacen', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('UbicacionesAlmacen', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('UbicacionesAlmacen', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('UbicacionesAlmacen', 'EliminarLogico'));

    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.pagina = 1;
        this.cargar();
      });

    this.cargarCatalogos();
    this.cargar();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cargar(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const padreId = typeof this.padreFiltro === 'number' ? this.padreFiltro : undefined;
    this.ubicacionService.buscar({
      buscar: this.buscar,
      almacenId: this.almacenId ?? undefined,
      ubicacionPadreId: padreId,
      soloRaiz: this.padreFiltro === 'raiz',
      tipo: this.tipo || undefined,
      activa: this.estado === 'activas' ? true : this.estado === 'inactivas' ? false : undefined,
      pagina: this.pagina,
      tamanoPagina: this.tamanoPagina
    }).subscribe({
      next: (res) => {
        this.ubicaciones.set(res.data.items);
        this.total.set(res.data.total);
        this.totalPaginas.set(res.data.totalPaginas);
        this.pagina = res.data.pagina;
        this.tamanoPagina = res.data.tamanoPagina;
        this.loading.set(false);
      },
      error: (err) => {
        this.ubicaciones.set([]);
        this.total.set(0);
        this.totalPaginas.set(0);
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudieron cargar las ubicaciones internas.');
      }
    });
  }

  cargarCatalogos(): void {
    this.loadingCatalogos.set(true);
    forkJoin({
      almacenes: this.almacenService.getActivos(),
      tipos: this.ubicacionService.getTipos()
    }).subscribe({
      next: ({ almacenes, tipos }) => {
        this.almacenes.set(almacenes.data);
        this.tipos.set(tipos.data);
        this.loadingCatalogos.set(false);
      },
      error: (err) => {
        this.almacenes.set([]);
        this.tipos.set([]);
        this.loadingCatalogos.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudieron cargar los catálogos de ubicaciones.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  onSearchChange(value: string): void {
    this.buscar = value;
    this.searchSubject.next(value.trim());
  }

  onAlmacenChange(): void {
    this.padreFiltro = 'todos';
    this.padresDisponibles.set([]);
    this.pagina = 1;
    if (this.almacenId) this.cargarPadres(this.almacenId);
    this.cargar();
  }

  aplicarFiltros(): void {
    this.pagina = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.buscar = '';
    this.estado = 'todos';
    this.almacenId = null;
    this.padreFiltro = 'todos';
    this.tipo = '';
    this.padresDisponibles.set([]);
    this.pagina = 1;
    this.tamanoPagina = 10;
    this.cargar();
  }

  onPageChange(event: PageEvent): void {
    this.pagina = event.pageIndex + 1;
    this.tamanoPagina = event.pageSize;
    this.cargar();
  }

  puedeCambiarEstado(ubicacion: UbicacionAlmacen): boolean {
    return !this.estaOperando(ubicacion.id) && (ubicacion.activa ? this.puedeDesactivar() : this.puedeActivar());
  }

  estaOperando(id: number): boolean {
    return this.operandoIds().has(id);
  }

  toggleActiva(ubicacion: UbicacionAlmacen): void {
    if (!this.puedeCambiarEstado(ubicacion)) return;

    this.marcarOperacion(ubicacion.id, true);
    const request$ = ubicacion.activa
      ? this.ubicacionService.desactivar(ubicacion.id)
      : this.ubicacionService.activar(ubicacion.id);

    request$.subscribe({
      next: (res) => {
        this.ubicaciones.update(items => items.map(item => item.id === ubicacion.id ? res.data : item));
        this.marcarOperacion(ubicacion.id, false);
        if (this.almacenId) this.cargarPadres(this.almacenId);
      },
      error: (err) => {
        this.marcarOperacion(ubicacion.id, false);
        this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado de la ubicación.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  async eliminar(ubicacion: UbicacionAlmacen): Promise<void> {
    if (this.estaOperando(ubicacion.id)) return;

    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar ubicación',
      mensaje: `Se ocultará “${ubicacion.codigo} · ${ubicacion.nombre}” sin borrar su historial. Si tiene ubicaciones hijas no eliminadas, la operación será rechazada.`,
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;

    this.marcarOperacion(ubicacion.id, true);
    this.ubicacionService.delete(ubicacion.id).subscribe({
      next: () => {
        this.marcarOperacion(ubicacion.id, false);
        if (this.ubicaciones().length === 1 && this.pagina > 1) this.pagina -= 1;
        this.snackBar.open('Ubicación eliminada correctamente.', 'Cerrar', { duration: 3500 });
        if (this.almacenId) this.cargarPadres(this.almacenId);
        this.cargar();
      },
      error: (err) => {
        this.marcarOperacion(ubicacion.id, false);
        this.snackBar.open(err.error?.message ?? 'No se pudo eliminar la ubicación.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  etiquetaPadre(ubicacion: UbicacionAlmacen): string {
    return ubicacion.ubicacionPadreId
      ? `${ubicacion.ubicacionPadreCodigo ?? '—'} · ${ubicacion.ubicacionPadreNombre ?? 'Padre'}`
      : 'Raíz';
  }

  private cargarPadres(almacenId: number): void {
    this.loadingPadres.set(true);
    this.ubicacionService.getActivas(almacenId).subscribe({
      next: (res) => {
        this.padresDisponibles.set(res.data);
        this.loadingPadres.set(false);
      },
      error: () => {
        this.padresDisponibles.set([]);
        this.loadingPadres.set(false);
      }
    });
  }

  private marcarOperacion(id: number, activo: boolean): void {
    const ids = new Set(this.operandoIds());
    if (activo) ids.add(id);
    else ids.delete(id);
    this.operandoIds.set(ids);
  }
}
