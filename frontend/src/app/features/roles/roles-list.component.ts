import { Component, OnInit, signal } from '@angular/core';

import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RolService } from '../../services/rol.service';
import { Rol } from '../../core/models/rol.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './roles-list.component.html',
  styleUrl: './roles-list.component.scss'
})
export class RolesListComponent implements OnInit {
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

  toggleActivo(rol: Rol): void {
    if (!this.puedeCambiarEstado(rol)) return;

    const accion$ = rol.activo
      ? this.rolService.desactivar(rol.id)
      : this.rolService.activar(rol.id);

    accion$.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado del rol.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(rol: Rol): Promise<void> {
    if (!this.puedeEliminar()) return;

    const confirmado = await this.alerts.confirmar({
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

    const nuevoNombre = await this.alerts.solicitarTexto({
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
