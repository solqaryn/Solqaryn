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
import { DescuentoService } from '../../services/descuento.service';
import { Descuento } from '../../core/models/descuento.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-descuentos-list',
  standalone: true,
  imports: [MatFormFieldModule, MatInputModule, MatSelectModule, ConsultaRegistroDirective, CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './descuentos-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './descuentos-list.component.scss'
})
export class DescuentosListComponent implements OnInit {
  private readonly acciones = crearAccionesInstitucionales('Descuentos');
  readonly cambiandoEstado = signal(false);
  readonly buscar = signal('');
  readonly estadoFiltro = signal<'todos' | 'activos' | 'inactivos'>('todos');
  readonly visibles = computed(() => {
    const termino = this.buscar().trim().toLocaleLowerCase('es-HN');
    return this.descuentos().filter(item => (!termino || item.nombre.toLocaleLowerCase('es-HN').includes(termino)) &&
      (this.estadoFiltro() === 'todos' || item.activo === (this.estadoFiltro() === 'activos')));
  });
  limpiarFiltros(): void { this.buscar.set(''); this.estadoFiltro.set('todos'); }

  async abrirEditor(id?: number): Promise<void> {
    if (await this.acciones.abrir(() => import('./descuento-form.component').then(m => m.DescuentoFormComponent), 'l', id ? 'Editar' : 'Crear', id)) this.cargar();
  }

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
    private snackBar: MatSnackBar
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

  async toggleActivo(descuento: Descuento, evento: MatSlideToggleChange): Promise<void> {
    evento.source.checked = descuento.activo;
    if (!this.puedeCambiarEstado(descuento) || this.cambiandoEstado()) return;
    this.cambiandoEstado.set(true);
    try {
      const accion = descuento.activo ? 'Desactivar' : 'Activar';
      if (!await this.acciones.confirmar(accion, { titulo: accion + ' registro', mensaje: '¿Deseas ' + accion.toLowerCase() + ' «' + descuento.nombre + '»?', confirmarTexto: accion })) return;
      await firstValueFrom(descuento.activo ? this.service.desactivar(descuento.id) : this.service.activar(descuento.id));
      this.cargar();
    } catch { this.snackBar.open('No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 }); }
    finally { this.cambiandoEstado.set(false); }
  }

  async eliminar(descuento: Descuento): Promise<void> {
    if (!this.puedeEliminar()) return;

    const confirmado = await this.acciones.confirmar('EliminarLogico', {
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
    const contexto = this.acciones.contexto();
    if (!this.acciones.vigente(contexto, 'Duplicar')) return;

    const nuevoNombre = await this.acciones.solicitarTexto('Duplicar', {
      titulo: 'Duplicar descuento',
      mensaje: `Define el nombre de la copia de "${descuento.nombre}".`,
      confirmarTexto: 'Crear copia',
      entrada: { etiqueta: 'Nombre del descuento', valor: `${descuento.nombre} (copia)`, requerida: true }
    });
    if (!nuevoNombre || !this.acciones.vigente(contexto, 'Duplicar')) return;

    this.service.duplicar(descuento.id, nuevoNombre).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo duplicar.', 'Cerrar', { duration: 5000 })
    });
  }
}
