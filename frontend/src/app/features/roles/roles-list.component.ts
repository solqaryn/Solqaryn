import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';
import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { Component, OnInit, signal, computed, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatDialog } from '@angular/material/dialog';
import { modalInstitucional } from '../../shared/modal-institucional/modal-institucional';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { firstValueFrom } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RolService } from '../../services/rol.service';
import { Rol } from '../../core/models/rol.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [MatFormFieldModule, MatInputModule, MatSelectModule, ConsultaRegistroDirective, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './roles-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './roles-list.component.scss'
})
export class RolesListComponent implements OnInit {
  private readonly acciones = crearAccionesInstitucionales('Roles');
  readonly buscar = signal('');
  readonly estadoFiltro = signal<'todos' | 'activos' | 'inactivos'>('todos');
  readonly visibles = computed(() => { const termino = this.buscar().trim().toLocaleLowerCase('es-HN'); return this.roles().filter(item => (!termino || item.nombre.toLocaleLowerCase('es-HN').includes(termino)) && (this.estadoFiltro() === 'todos' || item.activo === (this.estadoFiltro() === 'activos'))); });
  limpiarFiltros(): void { this.buscar.set(''); this.estadoFiltro.set('todos'); }

  private readonly dialog = inject(MatDialog);
  private readonly tenant = inject(TenantContextService);
  private readonly destroyRef = inject(DestroyRef);
  private modalPendiente = false;
  readonly cambiandoEstado = signal(false);
  readonly roles = signal<Rol[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeDuplicar = signal(false);

  constructor(
    private rolService: RolService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  async abrirRol(id?: number): Promise<void> {
    const permiso = id ? 'Editar' : 'Crear';
    if (this.modalPendiente || !this.permisosRuntime.puede('Roles', permiso)) return;
    const contexto = this.tenant.contextoVerificado();
    if (!contexto) return;
    this.modalPendiente = true;
    try {
      const { RolFormComponent } = await import('./rol-form.component');
      if (this.destroyRef.destroyed || contexto !== this.tenant.contextoVerificado() || !this.permisosRuntime.puede('Roles', permiso)) return;
      const guardado = await firstValueFrom(this.dialog.open(RolFormComponent, modalInstitucional('m', { data: { id }, ariaLabel: id ? 'Editar registro' : 'Crear registro' })).afterClosed());
      if (guardado === true && !this.destroyRef.destroyed) this.cargar();
    } catch { this.snackBar.open('No se pudo abrir el registro.', 'Cerrar', { duration: 5000 }); }
    finally { this.modalPendiente = false; }
  }

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Roles', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Roles', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Roles', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Roles', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Roles', 'EliminarLogico'));
    this.puedeDuplicar.set(this.permisosRuntime.puede('Roles', 'Duplicar'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.rolService.getAll().subscribe({
      next: (res) => {
        this.roles.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  puedeCambiarEstado(rol: Rol): boolean {
    return rol.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  async toggleActivo(rol: Rol, evento: MatSlideToggleChange): Promise<void> {
    evento.source.checked = rol.activo;
    if (!this.puedeCambiarEstado(rol) || this.cambiandoEstado()) return;
    const contexto = this.tenant.contextoVerificado();
    if (!contexto) return;
    this.cambiandoEstado.set(true);
    try {
      const accion = rol.activo ? 'Desactivar' : 'Activar';
      const confirmado = await this.alerts.confirmar({ titulo: accion + ' registro', mensaje: '¿Deseas ' + accion.toLowerCase() + ' «' + rol.nombre + '»?', confirmarTexto: accion });
      if (!confirmado || contexto !== this.tenant.contextoVerificado() || !this.permisosRuntime.puede('Roles', accion)) return;
      await firstValueFrom(rol.activo ? this.rolService.desactivar(rol.id) : this.rolService.activar(rol.id));
      this.cargar();
    } catch { this.snackBar.open('No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 }); }
    finally { this.cambiandoEstado.set(false); }
  }

  async eliminar(rol: Rol): Promise<void> {
    if (!this.puedeEliminar() || rol.esSistema || rol.cantidadUsuarios > 0) return;

    const confirmado = await this.acciones.confirmar('EliminarLogico', {
      titulo: 'Eliminar rol',
      mensaje: `Se eliminará el rol "${rol.nombre}".`,
      detalle: 'El rol quedará inactivo y la operación se registrará en auditoría.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar rol'
    });
    if (!confirmado) return;

    this.rolService.eliminarLogico(rol.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el rol.', 'Cerrar', { duration: 5000 })
    });
  }

  async duplicar(rol: Rol): Promise<void> {
    if (!this.puedeDuplicar()) return;

    const nuevoNombre = await this.acciones.solicitarTexto('Duplicar', {
      titulo: 'Duplicar rol',
      mensaje: `Define el nombre de la copia de "${rol.nombre}".`,
      confirmarTexto: 'Crear copia',
      entrada: { etiqueta: 'Nombre del nuevo rol', valor: `${rol.nombre} (copia)`, requerida: true }
    });
    if (!nuevoNombre) return;

    this.rolService.duplicar(rol.id, nuevoNombre).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo duplicar el rol.', 'Cerrar', { duration: 5000 })
    });
  }
}
