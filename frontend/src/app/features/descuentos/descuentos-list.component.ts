import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DescuentoService } from '../../services/descuento.service';
import { Descuento } from '../../core/models/descuento.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-descuentos-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './descuentos-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './descuentos-list.component.scss'
})
export class DescuentosListComponent implements OnInit {
  readonly descuentos = signal<Descuento[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeDuplicar = signal(false);

  constructor(
    private service: DescuentoService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Descuentos', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Descuentos', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Descuentos', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Descuentos', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Descuentos', 'EliminarLogico'));
    this.puedeDuplicar.set(this.permisosRuntime.puede('Descuentos', 'Duplicar'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (res) => { this.descuentos.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  puedeCambiarEstado(descuento: Descuento): boolean {
    return descuento.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  toggleActivo(descuento: Descuento): void {
    if (!this.puedeCambiarEstado(descuento)) return;

    const accion$ = descuento.activo
      ? this.service.desactivar(descuento.id)
      : this.service.activar(descuento.id);

    accion$.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(descuento: Descuento): Promise<void> {
    if (!this.puedeEliminar()) return;

    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar descuento',
      mensaje: `Se eliminará el descuento "${descuento.nombre}".`,
      detalle: 'Dejará de estar disponible para nuevas operaciones.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar descuento'
    });
    if (!confirmado) return;

    this.service.eliminarLogico(descuento.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar.', 'Cerrar', { duration: 5000 })
    });
  }

  async duplicar(descuento: Descuento): Promise<void> {
    if (!this.puedeDuplicar()) return;

    const nuevoNombre = await this.alerts.solicitarTexto({
      titulo: 'Duplicar descuento',
      mensaje: `Define el nombre de la copia de "${descuento.nombre}".`,
      confirmarTexto: 'Crear copia',
      entrada: { etiqueta: 'Nombre del descuento', valor: `${descuento.nombre} (copia)`, requerida: true }
    });
    if (!nuevoNombre) return;

    this.service.duplicar(descuento.id, nuevoNombre).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo duplicar.', 'Cerrar', { duration: 5000 })
    });
  }
}
