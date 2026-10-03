import { Component, OnInit, signal } from '@angular/core';

import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { TipoClienteService } from '../../services/tipo-cliente.service';
import { TipoCliente } from '../../core/models/tipo-cliente.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-tipo-clientes-list',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule, FormsModule],
  templateUrl: './tipo-clientes-list.component.html',
  styleUrl: './tipo-clientes-list.component.scss'
})
export class TipoClientesListComponent implements OnInit {
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
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
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

  toggleActivo(tipo: TipoCliente): void {
    if (!this.puedeCambiarEstado(tipo)) return;
    const operacion = tipo.activo
      ? this.tipoClienteService.desactivar(tipo.id)
      : this.tipoClienteService.activar(tipo.id);

    operacion.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 })
    });
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

    const confirmado = await this.alerts.confirmar({
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
