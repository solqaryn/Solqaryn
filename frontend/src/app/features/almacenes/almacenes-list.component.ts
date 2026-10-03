
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
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
import { Almacen, TipoAlmacenOpcion } from '../../core/models/almacen.model';
import { Sucursal } from '../../core/models/sucursal.model';
import { AlmacenService } from '../../services/almacen.service';
import { SucursalService } from '../../services/sucursal.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

type EstadoAlmacenFiltro = 'todos' | 'activos' | 'inactivos';

@Component({
  selector: 'app-almacenes-list',
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
  templateUrl: './almacenes-list.component.html',
  styleUrl: './almacenes-list.component.scss'
})
export class AlmacenesListComponent implements OnInit, OnDestroy {
  readonly almacenes = signal<Almacen[]>([]);
  readonly sucursales = signal<Sucursal[]>([]);
  readonly tipos = signal<TipoAlmacenOpcion[]>([]);
  readonly total = signal(0);
  readonly totalPaginas = signal(0);
  readonly loading = signal(true);
  readonly loadingCatalogos = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly operandoIds = signal<Set<number>>(new Set<number>());

  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  buscar = '';
  estado: EstadoAlmacenFiltro = 'todos';
  sucursalId: number | null = null;
  tipo = '';
  pagina = 1;
  tamanoPagina = 10;

  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private almacenService: AlmacenService,
    private sucursalService: SucursalService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Almacenes', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Almacenes', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Almacenes', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Almacenes', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Almacenes', 'EliminarLogico'));

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

    this.almacenService.buscar({
      buscar: this.buscar,
      activo: this.estado === 'activos' ? true : this.estado === 'inactivos' ? false : undefined,
      sucursalId: this.sucursalId && this.sucursalId > 0 ? this.sucursalId : undefined,
      tipo: this.tipo || undefined,
      pagina: this.pagina,
      tamanoPagina: this.tamanoPagina
    }).subscribe({
      next: (res) => {
        this.almacenes.set(res.data.items);
        this.total.set(res.data.total);
        this.totalPaginas.set(res.data.totalPaginas);
        this.pagina = res.data.pagina;
        this.tamanoPagina = res.data.tamanoPagina;
        this.loading.set(false);
      },
      error: (err) => {
        this.almacenes.set([]);
        this.total.set(0);
        this.totalPaginas.set(0);
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudieron cargar los almacenes.');
      }
    });
  }

  cargarCatalogos(): void {
    this.loadingCatalogos.set(true);
    forkJoin({
      sucursales: this.sucursalService.getActivas(),
      tipos: this.almacenService.getTipos()
    }).subscribe({
      next: ({ sucursales, tipos }) => {
        this.sucursales.set(sucursales.data);
        this.tipos.set(tipos.data);
        this.loadingCatalogos.set(false);
      },
      error: (err) => {
        this.sucursales.set([]);
        this.tipos.set([]);
        this.loadingCatalogos.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudieron cargar los catálogos de almacenes.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  onSearchChange(value: string): void {
    this.buscar = value;
    this.searchSubject.next(value.trim());
  }

  aplicarFiltros(): void {
    this.pagina = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.buscar = '';
    this.estado = 'todos';
    this.sucursalId = null;
    this.tipo = '';
    this.pagina = 1;
    this.tamanoPagina = 10;
    this.cargar();
  }

  onPageChange(event: PageEvent): void {
    this.pagina = event.pageIndex + 1;
    this.tamanoPagina = event.pageSize;
    this.cargar();
  }

  puedeCambiarEstado(almacen: Almacen): boolean {
    return !this.estaOperando(almacen.id) && (almacen.activo ? this.puedeDesactivar() : this.puedeActivar());
  }

  estaOperando(id: number): boolean {
    return this.operandoIds().has(id);
  }

  toggleActivo(almacen: Almacen): void {
    if (!this.puedeCambiarEstado(almacen)) return;

    this.marcarOperacion(almacen.id, true);
    const operacion = almacen.activo
      ? this.almacenService.desactivar(almacen.id)
      : this.almacenService.activar(almacen.id);

    operacion.subscribe({
      next: (res) => {
        this.almacenes.update(items => items.map(item => item.id === almacen.id ? res.data : item));
        this.marcarOperacion(almacen.id, false);
      },
      error: (err) => {
        this.marcarOperacion(almacen.id, false);
        this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado del almacén.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  async eliminar(almacen: Almacen): Promise<void> {
    if (this.estaOperando(almacen.id)) return;

    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar almacén',
      mensaje: `Se ocultará “${almacen.codigo} · ${almacen.nombre}” sin borrar su historial.`,
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;

    this.marcarOperacion(almacen.id, true);
    this.almacenService.delete(almacen.id).subscribe({
      next: () => {
        this.marcarOperacion(almacen.id, false);
        if (this.almacenes().length === 1 && this.pagina > 1) this.pagina -= 1;
        this.snackBar.open('Almacén eliminado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => {
        this.marcarOperacion(almacen.id, false);
        this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el almacén.', 'Cerrar', { duration: 5000 });
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
