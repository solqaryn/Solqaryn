import { Component, OnInit, signal } from '@angular/core';

import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ImpuestoService } from '../../services/impuesto.service';
import { Impuesto } from '../../core/models/impuesto.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-impuestos-list',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './impuestos-list.component.html',
  styleUrl: './impuestos-list.component.scss'
})
export class ImpuestosListComponent implements OnInit {
  readonly impuestos = signal<Impuesto[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeDuplicar = signal(false);

  constructor(
    private service: ImpuestoService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Impuestos', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Impuestos', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Impuestos', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Impuestos', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Impuestos', 'EliminarLogico'));
    this.puedeDuplicar.set(this.permisosRuntime.puede('Impuestos', 'Duplicar'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (res) => { this.impuestos.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  puedeCambiarEstado(impuesto: Impuesto): boolean {
    return impuesto.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  toggleActivo(impuesto: Impuesto): void {
    if (!this.puedeCambiarEstado(impuesto)) return;

    const accion$ = impuesto.activo
      ? this.service.desactivar(impuesto.id)
      : this.service.activar(impuesto.id);

    accion$.subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(impuesto: Impuesto): Promise<void> {
    if (!this.puedeEliminar()) return;

    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar impuesto',
      mensaje: `Se eliminará el impuesto "${impuesto.nombre}".`,
      detalle: 'No se aplicará a nuevas operaciones.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar impuesto'
    });
    if (!confirmado) return;

    this.service.eliminarLogico(impuesto.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar.', 'Cerrar', { duration: 5000 })
    });
  }

  async duplicar(impuesto: Impuesto): Promise<void> {
    if (!this.puedeDuplicar()) return;

    const nuevoNombre = await this.alerts.solicitarTexto({
      titulo: 'Duplicar impuesto',
      mensaje: `Define el nombre de la copia de "${impuesto.nombre}".`,
      confirmarTexto: 'Continuar',
      entrada: { etiqueta: 'Nombre del impuesto', valor: `${impuesto.nombre} (copia)`, requerida: true }
    });
    if (!nuevoNombre) return;

    const nuevoCodigo = await this.alerts.solicitarTexto({
      titulo: 'Código del impuesto',
      mensaje: 'El código debe ser único en el sistema.',
      confirmarTexto: 'Crear copia',
      entrada: { etiqueta: 'Código único', valor: `${impuesto.codigo}-COPIA`, requerida: true }
    });
    if (!nuevoCodigo) return;

    this.service.duplicar(impuesto.id, nuevoNombre, nuevoCodigo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo duplicar.', 'Cerrar', { duration: 5000 })
    });
  }
}
