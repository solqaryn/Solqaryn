import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { FormsModule } from '@angular/forms';
import { debounceTime, Subject } from 'rxjs';
import { ClienteService } from '../../services/cliente.service';
import { TipoClienteService } from '../../services/tipo-cliente.service';
import { Cliente } from '../../core/models/cliente.model';
import { TipoCliente } from '../../core/models/tipo-cliente.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

type EstadoClienteFiltro = 'todos' | 'activos' | 'inactivos';
type OrdenCliente = 'nombre' | 'totalVentas' | 'totalVendido';

interface SegmentoClienteResumen {
  id: number;
  nombre: string;
  colorHex: string;
  activo: boolean;
  totalClientes: number;
  clientesActivos: number;
  totalVentas: number;
  totalVendido: number;
}

@Component({
  selector: 'app-clientes-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule,
    MatSlideToggleModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatPaginatorModule, FormsModule
  ],
  templateUrl: './clientes-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './clientes-list.component.scss'
})
export class ClientesListComponent implements OnInit {
  readonly clientes = signal<Cliente[]>([]);
  readonly tiposCliente = signal<TipoCliente[]>([]);
  readonly resumenSegmentos = signal<SegmentoClienteResumen[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeVerTiposCliente = signal(false);

  search = '';
  estado: EstadoClienteFiltro = 'todos';
  tipoClienteId = 0;
  sortBy: OrdenCliente = 'nombre';
  sortDirection: 'asc' | 'desc' = 'asc';
  page = 1;
  pageSize = 10;

  private clientesOrigen: Cliente[] = [];
  private clientesExportables: Cliente[] = [];
  private readonly navigationDefaults = {
    search: '', estado: 'todos', tipoClienteId: 0, sortBy: 'nombre', sortDirection: 'asc', page: 1, pageSize: 10
  };
  private readonly searchSubject = new Subject<string>();

  constructor(
    private clienteService: ClienteService,
    private tipoClienteService: TipoClienteService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService,
    private route: ActivatedRoute,
    private navigationState: ListNavigationStateService
  ) {
    this.searchSubject.pipe(debounceTime(250)).subscribe(() => { this.page = 1; this.aplicarVista(); });
  }

  ngOnInit(): void {
    const state = this.navigationState.restore('clientes', this.route, this.navigationDefaults);
    this.search = state.search;
    this.estado = ['todos', 'activos', 'inactivos'].includes(state.estado) ? state.estado as EstadoClienteFiltro : 'todos';
    this.tipoClienteId = Number.isFinite(Number(state.tipoClienteId)) ? Math.max(0, Math.trunc(Number(state.tipoClienteId))) : 0;
    this.sortBy = ['nombre', 'totalVentas', 'totalVendido'].includes(state.sortBy) ? state.sortBy as OrdenCliente : 'nombre';
    this.sortDirection = state.sortDirection === 'desc' ? 'desc' : 'asc';
    this.page = Math.max(1, Math.trunc(state.page));
    this.pageSize = [10, 25, 50].includes(state.pageSize) ? state.pageSize : 10;

    this.puedeCrear.set(this.permisosRuntime.puede('Clientes', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Clientes', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Clientes', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Clientes', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Clientes', 'EliminarLogico'));
    this.puedeVerTiposCliente.set(this.permisosRuntime.puede('TiposClientes', 'Ver'));

    this.persistirEstado();
    this.cargarTiposCliente();
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.clienteService.getAll().subscribe({
      next: (res) => {
        this.clientesOrigen = res.data;
        this.completarTiposDesdeClientes();
        this.aplicarVista();
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.searchSubject.next(value);
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.aplicarVista();
  }

  seleccionarSegmento(tipoClienteId: number): void {
    this.tipoClienteId = this.tipoClienteId === tipoClienteId ? 0 : tipoClienteId;
    this.page = 1;
    this.aplicarVista();
  }

  limpiarFiltros(): void {
    this.navigationState.clear('clientes');
    this.search = '';
    this.estado = 'todos';
    this.tipoClienteId = 0;
    this.sortBy = 'nombre';
    this.sortDirection = 'asc';
    this.page = 1;
    this.pageSize = 10;
    this.aplicarVista();
  }

  ordenarPor(campo: OrdenCliente): void {
    if (this.sortBy === campo) this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = campo; this.sortDirection = 'asc'; }
    this.page = 1;
    this.aplicarVista();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.aplicarVista();
  }

  exportarCsv(): void {
    if (this.clientesExportables.length === 0) {
      this.snackBar.open('No hay clientes para exportar con los filtros actuales.', 'Cerrar', { duration: 3000 });
      return;
    }

    const cabeceras = ['Nombre', 'Telefono', 'Identidad/RTN', 'Correo', 'Direccion', 'Clasificacion', 'Estado', 'Ventas', 'Total vendido'];
    const filas = this.clientesExportables.map((c) => [
      c.nombre,
      c.telefono ?? '',
      c.identidadORTN ?? '',
      c.correo ?? '',
      c.direccion ?? '',
      c.tipoClienteNombre,
      c.activo ? 'Activo' : 'Inactivo',
      String(c.totalVentas),
      c.totalVendido.toFixed(2)
    ]);
    const csv = [cabeceras, ...filas]
      .map((fila) => fila.map((valor) => this.csvCell(valor)).join(','))
      .join('\r\n');

    const blob = new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = `clientes-segmentacion-${new Date().toISOString().slice(0, 10)}.csv`;
    enlace.click();
    URL.revokeObjectURL(url);
  }

  puedeCambiarEstado(cliente: Cliente): boolean {
    return cliente.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  toggleActivo(c: Cliente): void {
    if (!this.puedeCambiarEstado(c)) return;
    const operacion = c.activo
      ? this.clienteService.desactivar(c.id)
      : this.clienteService.activar(c.id);

    operacion.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado del cliente.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(c: Cliente): Promise<void> {
    const confirmado = await this.alerts.confirmar({ titulo: 'Eliminar cliente', mensaje: `Se ocultará a "${c.nombre}" sin borrar sus ventas históricas.`, tipo: 'peligro', confirmarTexto: 'Eliminar' });
    if (!confirmado) return;

    this.clienteService.delete(c.id).subscribe({
      next: () => {
        this.snackBar.open('Cliente eliminado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el cliente.', 'Cerrar', { duration: 5000 })
    });
  }

  private cargarTiposCliente(): void {
    if (!this.puedeVerTiposCliente()) return;

    this.tipoClienteService.getAll().subscribe({
      next: (res) => {
        this.tiposCliente.set(res.data);
        this.recalcularResumenSegmentos();
      },
      error: () => this.completarTiposDesdeClientes()
    });
  }

  private completarTiposDesdeClientes(): void {
    const existentes = new Map(this.tiposCliente().map((tipo) => [tipo.id, tipo]));
    for (const cliente of this.clientesOrigen) {
      if (existentes.has(cliente.tipoClienteId)) continue;
      existentes.set(cliente.tipoClienteId, {
        id: cliente.tipoClienteId,
        codigo: '',
        esSistema: false,
        nombre: cliente.tipoClienteNombre,
        nombreNormalizado: cliente.tipoClienteNombre.toUpperCase(),
        colorHex: cliente.tipoClienteColorHex,
        activo: true,
        orden: 9999,
        esPredeterminado: false,
        totalClientesAsignados: 0
      });
    }
    this.tiposCliente.set([...existentes.values()].sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre, 'es')));
    this.recalcularResumenSegmentos();
  }

  private aplicarVista(): void {
    let data = this.filtrarBase(true);
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    data = [...data].sort((a, b) => {
      if (this.sortBy === 'totalVentas') return (a.totalVentas - b.totalVentas) * direction;
      if (this.sortBy === 'totalVendido') return (a.totalVendido - b.totalVendido) * direction;
      return a.nombre.localeCompare(b.nombre, 'es', { sensitivity: 'base' }) * direction;
    });

    this.clientesExportables = data;
    this.totalCount.set(data.length);
    const maxPage = Math.max(1, Math.ceil(data.length / this.pageSize));
    this.page = Math.min(Math.max(1, this.page), maxPage);
    const start = (this.page - 1) * this.pageSize;
    this.clientes.set(data.slice(start, start + this.pageSize));
    this.recalcularResumenSegmentos();
    this.persistirEstado();
  }

  private filtrarBase(incluirTipo: boolean): Cliente[] {
    const term = this.search.trim().toLowerCase();
    return this.clientesOrigen.filter((cliente) => {
      const coincideEstado = this.estado === 'todos'
        || (this.estado === 'activos' && cliente.activo)
        || (this.estado === 'inactivos' && !cliente.activo);
      if (!coincideEstado) return false;
      if (incluirTipo && this.tipoClienteId > 0 && cliente.tipoClienteId !== this.tipoClienteId) return false;
      if (!term) return true;
      return [cliente.nombre, cliente.telefono, cliente.identidadORTN, cliente.correo, cliente.tipoClienteNombre]
        .some((value) => value?.toLowerCase().includes(term));
    });
  }

  private recalcularResumenSegmentos(): void {
    const base = this.filtrarBase(false);
    const resumen = this.tiposCliente().map((tipo) => {
      const clientesTipo = base.filter((cliente) => cliente.tipoClienteId === tipo.id);
      return {
        id: tipo.id,
        nombre: tipo.nombre,
        colorHex: tipo.colorHex,
        activo: tipo.activo,
        totalClientes: clientesTipo.length,
        clientesActivos: clientesTipo.filter((cliente) => cliente.activo).length,
        totalVentas: clientesTipo.reduce((total, cliente) => total + cliente.totalVentas, 0),
        totalVendido: clientesTipo.reduce((total, cliente) => total + cliente.totalVendido, 0)
      };
    });
    this.resumenSegmentos.set(resumen);
  }

  private csvCell(value: string): string {
    return `"${value.replace(/"/g, '""')}"`;
  }

  private persistirEstado(): void {
    this.navigationState.persist('clientes', this.route, {
      search: this.search,
      estado: this.estado,
      tipoClienteId: this.tipoClienteId,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection,
      page: this.page,
      pageSize: this.pageSize
    }, this.navigationDefaults);
  }
}
