import { crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';
import { firstValueFrom } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { Component, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { ProveedorService } from '../../services/proveedor.service';
import { Proveedor } from '../../core/models/proveedor.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-proveedores-list',
  standalone: true,
  imports: [MatFormFieldModule, MatInputModule, MatSelectModule, ConsultaRegistroDirective, CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule, FormsModule],
  templateUrl: './proveedores-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './proveedores-list.component.scss'
})
export class ProveedoresListComponent implements OnInit {
  private readonly acciones = crearAccionesInstitucionales('Proveedores');
  readonly cambiandoEstado = signal(false);
  readonly buscar = signal('');
  readonly estadoFiltro = signal<'todos' | 'activos' | 'inactivos'>('todos');
  readonly visibles = computed(() => {
    const termino = this.buscar().trim().toLocaleLowerCase('es-HN');
    return this.proveedores().filter(item => (!termino || item.nombre.toLocaleLowerCase('es-HN').includes(termino)) &&
      (this.estadoFiltro() === 'todos' || item.activo === (this.estadoFiltro() === 'activos')));
  });
  limpiarFiltros(): void { this.buscar.set(''); this.estadoFiltro.set('todos'); }

  async abrirEditor(id?: number): Promise<void> {
    if (await this.acciones.abrir(() => import('./proveedor-form.component').then(m => m.ProveedorFormComponent), 'm', id ? 'Editar' : 'Crear', id)) this.cargar();
  }

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
    private snackBar: MatSnackBar
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

  async toggleActivo(p: Proveedor, evento: MatSlideToggleChange): Promise<void> {
    evento.source.checked = p.activo;
    if (!this.puedeCambiarEstado(p) || this.cambiandoEstado()) return;
    this.cambiandoEstado.set(true);
    try {
      const accion = p.activo ? 'Desactivar' : 'Activar';
      if (!await this.acciones.confirmar(accion, { titulo: accion + ' registro', mensaje: '¿Deseas ' + accion.toLowerCase() + ' «' + p.nombre + '»?', confirmarTexto: accion })) return;
      await firstValueFrom(p.activo ? this.proveedorService.desactivar(p.id) : this.proveedorService.activar(p.id));
      this.cargar();
    } catch { this.snackBar.open('No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 }); }
    finally { this.cambiandoEstado.set(false); }
  }

  async eliminar(p: Proveedor): Promise<void> {
    const confirmado = await this.acciones.confirmar('EliminarLogico', { titulo: 'Eliminar proveedor', mensaje: `Se ocultará a "${p.nombre}" sin borrar sus compras históricas.`, tipo: 'peligro', confirmarTexto: 'Eliminar' });
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
