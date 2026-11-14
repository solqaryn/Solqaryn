import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { ProveedorService } from '../../services/proveedor.service';
import { Proveedor } from '../../core/models/proveedor.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-proveedores-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule, FormsModule],
  templateUrl: './proveedores-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './proveedores-list.component.scss'
})
export class ProveedoresListComponent implements OnInit {
  readonly proveedores = signal<Proveedor[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  constructor(
    private proveedorService: ProveedorService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Proveedores', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Proveedores', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Proveedores', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Proveedores', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Proveedores', 'EliminarLogico'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.proveedorService.getAll().subscribe({
      next: (res) => { this.proveedores.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  puedeCambiarEstado(proveedor: Proveedor): boolean {
    return proveedor.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  toggleActivo(p: Proveedor): void {
    if (!this.puedeCambiarEstado(p)) return;
    const operacion = p.activo
      ? this.proveedorService.desactivar(p.id)
      : this.proveedorService.activar(p.id);

    operacion.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado del proveedor.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(p: Proveedor): Promise<void> {
    const confirmado = await this.alerts.confirmar({ titulo: 'Eliminar proveedor', mensaje: `Se ocultará a "${p.nombre}" sin borrar sus compras históricas.`, tipo: 'peligro', confirmarTexto: 'Eliminar' });
    if (!confirmado) return;

    this.proveedorService.delete(p.id).subscribe({
      next: () => {
        this.snackBar.open('Proveedor eliminado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el proveedor.', 'Cerrar', { duration: 5000 })
    });
  }
}
