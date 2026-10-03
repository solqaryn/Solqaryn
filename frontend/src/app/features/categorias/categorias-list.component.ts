import { Component, OnInit, signal } from '@angular/core';

import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CategoriaService } from '../../services/categoria.service';
import { Categoria } from '../../core/models/categoria.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-categorias-list',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './categorias-list.component.html',
  styleUrl: './categorias-list.component.scss'
})
export class CategoriasListComponent implements OnInit {
  readonly categorias = signal<Categoria[]>([]);
  readonly loading = signal(true);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  constructor(
    private categoriaService: CategoriaService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.puedeCrear.set(this.permisosRuntime.puede('Categorias', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Categorias', 'Editar'));
    this.puedeActivar.set(this.permisosRuntime.puede('Categorias', 'Activar'));
    this.puedeDesactivar.set(this.permisosRuntime.puede('Categorias', 'Desactivar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Categorias', 'EliminarLogico'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.categoriaService.getAll().subscribe({
      next: (res) => {
        this.categorias.set(res.data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('No se pudieron cargar las categorías.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  puedeCambiarEstado(categoria: Categoria): boolean {
    return categoria.activa ? this.puedeDesactivar() : this.puedeActivar();
  }

  toggleActiva(categoria: Categoria): void {
    if (!this.puedeCambiarEstado(categoria)) return;
    const operacion = categoria.activa
      ? this.categoriaService.desactivar(categoria.id)
      : this.categoriaService.activar(categoria.id);

    operacion.subscribe({
      next: (res) => {
        this.categorias.update(items => items.map(item => item.id === categoria.id ? res.data : item));
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo cambiar el estado de la categoría.', 'Cerrar', { duration: 5000 })
    });
  }

  async eliminar(categoria: Categoria): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar categoría',
      mensaje: `Se ocultará “${categoria.nombre}” sin borrar el historial relacionado.`,
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;

    this.categoriaService.delete(categoria.id).subscribe({
      next: () => {
        // La lista se actualiza en memoria en el mismo instante. Una recarga
        // posterior obtiene el mismo resultado gracias al filtro soft-delete.
        this.categorias.update(items => items.filter(item => item.id !== categoria.id));
        this.snackBar.open('Categoría eliminada correctamente.', 'Cerrar', { duration: 3500 });
      },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo eliminar la categoría.', 'Cerrar', { duration: 5000 })
    });
  }
}
