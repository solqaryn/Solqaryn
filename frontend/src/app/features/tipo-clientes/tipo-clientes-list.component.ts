import { crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';
import { firstValueFrom } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { Component, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { TipoClienteService } from '../../services/tipo-cliente.service';
import { TipoCliente } from '../../core/models/tipo-cliente.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-tipo-clientes-list',
  standalone: true,
  imports: [MatFormFieldModule, MatInputModule, MatSelectModule, ConsultaRegistroDirective, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule, FormsModule],
  templateUrl: './tipo-clientes-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './tipo-clientes-list.component.scss'
})
export class TipoClientesListComponent implements OnInit {
  private readonly acciones = crearAccionesInstitucionales('TiposClientes');
  readonly cambiandoEstado = signal(false);
  readonly buscar = signal('');
  readonly estadoFiltro = signal<'todos' | 'activos' | 'inactivos'>('todos');
  readonly visibles = computed(() => {
    const termino = this.buscar().trim().toLocaleLowerCase('es-HN');
    return this.tipos().filter(item => (!termino || item.nombre.toLocaleLowerCase('es-HN').includes(termino) || item.codigo.toLocaleLowerCase('es-HN').includes(termino)) &&
      (this.estadoFiltro() === 'todos' || item.activo === (this.estadoFiltro() === 'activos')));
  });
  limpiarFiltros(): void { this.buscar.set(''); this.estadoFiltro.set('todos'); }

  async abrirEditor(id?: number): Promise<void> {
    if (await this.acciones.abrir(() => import('./tipo-cliente-form.component').then(m => m.TipoClienteFormComponent), 'm', id ? 'Editar' : 'Crear', id)) this.cargar();
  }

  readonly tipos = signal<TipoCliente[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  constructor(
    private tipoClienteService: TipoClienteService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('TiposClientes', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('TiposClientes', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('TiposClientes', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('TiposClientes', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('TiposClientes', 'EliminarLogico'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.tipoClienteService.getAll().subscribe({
      next: (res) => { this.tipos.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  puedeCambiarEstado(tipo: TipoCliente): boolean {
    if (tipo.codigo === 'SIN_CLASIFICAR' || tipo.esPredeterminado) return false;
    return tipo.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  async toggleActivo(tipo: TipoCliente, evento: MatSlideToggleChange): Promise<void> {
    evento.source.checked = tipo.activo;
    if (!this.puedeCambiarEstado(tipo) || this.cambiandoEstado()) return;
    this.cambiandoEstado.set(true);
    
    try {
      const accion = tipo.activo ? 'Desactivar' : 'Activar';
      if (!await this.acciones.confirmar(accion, { titulo: accion + ' registro', mensaje: '¿Deseas ' + accion.toLowerCase() + ' «' + tipo.nombre + '»?', confirmarTexto: accion })) return;
      await firstValueFrom(tipo.activo ? this.tipoClienteService.desactivar(tipo.id) : this.tipoClienteService.activar(tipo.id));
      this.cargar();
    } catch { this.snackBar.open('No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 }); }
    finally { this.cambiandoEstado.set(false); }
  }

  async eliminar(tipo: TipoCliente): Promise<void> {
    if (tipo.esSistema) {
      this.snackBar.open('Los tipos de cliente del sistema no pueden eliminarse.', 'Cerrar', { duration: 3000 });
      return;
    }
    if (tipo.esPredeterminado) {
      this.snackBar.open('El tipo de cliente predeterminado no puede eliminarse.', 'Cerrar', { duration: 3000 });
      return;
    }
    if (tipo.totalClientesAsignados > 0) {
      this.snackBar.open('No se puede eliminar porque tiene clientes asignados.', 'Cerrar', { duration: 4000 });
      return;
    }

    const confirmado = await this.acciones.confirmar('EliminarLogico', {
      titulo: 'Eliminar clasificación',
      mensaje: `¿Desea eliminar la clasificación de cliente "${tipo.nombre}"? Esta acción no se puede deshacer.`,
      tipo: 'peligro',
      confirmarTexto: 'Eliminar'
    });
    if (!confirmado) return;

    this.tipoClienteService.delete(tipo.id).subscribe({
      next: () => {
        this.snackBar.open('Clasificación eliminada correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar la clasificación.', 'Cerrar', { duration: 5000 })
    });
  }
}

// Compatibility export for the historical route symbol; keeps the canonical class name unchanged.
export { TipoClientesListComponent as TiposClientesListComponent };
