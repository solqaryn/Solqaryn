import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { Component, OnInit, signal, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatDialog } from '@angular/material/dialog';
import { modalInstitucional } from '../../shared/modal-institucional/modal-institucional';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { firstValueFrom } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CategoriaService } from '../../services/categoria.service';
import { Categoria } from '../../core/models/categoria.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-categorias-list',
  standalone: true,
  imports: [ConsultaRegistroDirective, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './categorias-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './categorias-list.component.scss'
})
export class CategoriasListComponent implements OnInit {
  private readonly dialog = inject(MatDialog);
  private readonly tenant = inject(TenantContextService);
  private readonly destroyRef = inject(DestroyRef);
  private modalPendiente = false;
  readonly cambiandoEstado = signal(false);
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

  async abrirCategoria(id?: number): Promise<void> {
    const permiso = id ? 'Editar' : 'Crear';
    if (this.modalPendiente || !this.permisosRuntime.puede('Categorias', permiso)) return;
    const contexto = this.tenant.contextoVerificado();
    if (!contexto) return;
    this.modalPendiente = true;
    try {
      const { CategoriaFormComponent } = await import('./categoria-form.component');
      if (this.destroyRef.destroyed || contexto !== this.tenant.contextoVerificado() || !this.permisosRuntime.puede('Categorias', permiso)) return;
      const guardado = await firstValueFrom(this.dialog.open(CategoriaFormComponent, modalInstitucional('l', { data: { id }, ariaLabel: id ? 'Editar registro' : 'Crear registro' })).afterClosed());
      if (guardado === true && !this.destroyRef.destroyed) this.cargar();
    } catch { this.snackBar.open('No se pudo abrir el registro.', 'Cerrar', { duration: 5000 }); }
    finally { this.modalPendiente = false; }
  }

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

  async toggleActiva(categoria: Categoria, evento: MatSlideToggleChange): Promise<void> {
    evento.source.checked = categoria.activa;
    if (!this.puedeCambiarEstado(categoria) || this.cambiandoEstado()) return;
    const contexto = this.tenant.contextoVerificado();
    if (!contexto) return;
    this.cambiandoEstado.set(true);
    try {
      const accion = categoria.activa ? 'Desactivar' : 'Activar';
      const confirmado = await this.alerts.confirmar({ titulo: accion + ' registro', mensaje: '¿Deseas ' + accion.toLowerCase() + ' «' + categoria.nombre + '»?', confirmarTexto: accion });
      if (!confirmado || contexto !== this.tenant.contextoVerificado() || !this.permisosRuntime.puede('Categorias', accion)) return;
      const res = await firstValueFrom(categoria.activa ? this.categoriaService.desactivar(categoria.id) : this.categoriaService.activar(categoria.id));
      this.categorias.update(items => items.map(item => item.id === categoria.id ? res.data : item));
    } catch { this.snackBar.open('No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 }); }
    finally { this.cambiandoEstado.set(false); }
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
