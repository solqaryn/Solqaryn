
import { HttpClient } from '@angular/common/http';
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
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ApiResponse } from '../../core/models/api-response.model';
import { Sucursal } from '../../core/models/sucursal.model';
import { SucursalService } from '../../services/sucursal.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

type EstadoSucursalFiltro = 'todas' | 'activas' | 'inactivas';

interface EmpresaOpcion {
  id: number;
  nombre: string;
  activa: boolean;
}

@Component({
  selector: 'app-sucursales-list',
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
  templateUrl: './sucursales-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './sucursales-list.component.scss'
})
export class SucursalesListComponent implements OnInit, OnDestroy {
  readonly sucursales = signal<Sucursal[]>([]);
  readonly total = signal(0);
  readonly totalPaginas = signal(0);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly empresas = signal<EmpresaOpcion[]>([]);
  readonly empresasLoading = signal(true);
  readonly empresasError = signal<string | null>(null);
  readonly operandoIds = signal<Set<number>>(new Set<number>());

  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  buscar = '';
  estado: EstadoSucursalFiltro = 'todas';
  empresaId: number | null = null;
  pagina = 1;
  tamanoPagina = 10;

  private readonly empresasUrl = `${environment.apiUrl}/empresas`;
  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private sucursalService: SucursalService,
    private http: HttpClient,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Sucursales', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Sucursales', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Sucursales', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Sucursales', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Sucursales', 'EliminarLogico'));

    this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.pagina = 1;
        this.cargar();
      });

    this.cargarEmpresas();
    this.cargar();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cargarEmpresas(): void {
    this.empresasLoading.set(true);
    this.empresasError.set(null);
    this.http.get<ApiResponse<EmpresaOpcion[]>>(this.empresasUrl).subscribe({
      next: (res) => {
        this.empresas.set(res.data ?? []);
        this.empresasLoading.set(false);
      },
      error: () => {
        this.empresas.set([]);
        this.empresasLoading.set(false);
        this.empresasError.set('No se pudieron cargar las empresas para filtrar.');
      }
    });
  }

  nombreEmpresa(empresaId: number | null | undefined): string {
    if (!empresaId) return 'Sin asignar (legado)';
    return this.empresas().find(empresa => empresa.id === empresaId)?.nombre ?? `Empresa #${empresaId}`;
  }

  cargar(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.sucursalService.buscar({
      buscar: this.buscar,
      activa: this.estado === 'activas' ? true : this.estado === 'inactivas' ? false : undefined,
      empresaId: this.empresaId && this.empresaId > 0 ? this.empresaId : undefined,
      pagina: this.pagina,
      tamanoPagina: this.tamanoPagina
    }).subscribe({
      next: (res) => {
        this.sucursales.set(res.data.items);
        this.total.set(res.data.total);
        this.totalPaginas.set(res.data.totalPaginas);
        this.pagina = res.data.pagina;
        this.tamanoPagina = res.data.tamanoPagina;
        this.loading.set(false);
      },
      error: (err) => {
        this.sucursales.set([]);
        this.total.set(0);
        this.totalPaginas.set(0);
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudieron cargar las sucursales.');
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
    this.estado = 'todas';
    this.empresaId = null;
    this.pagina = 1;
    this.tamanoPagina = 10;
    this.cargar();
  }

  onPageChange(event: PageEvent): void {
    this.pagina = event.pageIndex + 1;
    this.tamanoPagina = event.pageSize;
    this.cargar();
  }

  puedeCambiarEstado(sucursal: Sucursal): boolean {
    return !this.estaOperando(sucursal.id) && (sucursal.activa ? this.puedeDesactivar() : this.puedeActivar());
  }

  estaOperando(id: number): boolean {
    return this.operandoIds().has(id);
  }

  toggleActiva(sucursal: Sucursal): void {
    if (!this.puedeCambiarEstado(sucursal)) return;

    this.marcarOperacion(sucursal.id, true);
    const operacion = sucursal.activa
      ? this.sucursalService.desactivar(sucursal.id)
      : this.sucursalService.activar(sucursal.id);

    operacion.subscribe({
      next: (res) => {
        this.sucursales.update(items => items.map(item => item.id === sucursal.id ? res.data : item));
        this.marcarOperacion(sucursal.id, false);
      },
      error: (err) => {
        this.marcarOperacion(sucursal.id, false);
        this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado de la sucursal.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  async eliminar(sucursal: Sucursal): Promise<void> {
    if (this.estaOperando(sucursal.id)) return;

    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar sucursal',
      mensaje: `Se ocultará “${sucursal.codigo} · ${sucursal.nombre}” sin borrar su historial.`,
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;

    this.marcarOperacion(sucursal.id, true);
    this.sucursalService.delete(sucursal.id).subscribe({
      next: () => {
        this.marcarOperacion(sucursal.id, false);
        if (this.sucursales().length === 1 && this.pagina > 1) this.pagina -= 1;
        this.snackBar.open('Sucursal eliminada correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => {
        this.marcarOperacion(sucursal.id, false);
        this.snackBar.open(err.error?.message ?? 'No se pudo eliminar la sucursal.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  private marcarOperacion(id: number, activa: boolean): void {
    const ids = new Set(this.operandoIds());
    if (activa) ids.add(id);
    else ids.delete(id);
    this.operandoIds.set(ids);
  }
}
