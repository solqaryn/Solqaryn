import { crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';
import { firstValueFrom } from 'rxjs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { Component, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ImpuestoService } from '../../services/impuesto.service';
import { Impuesto } from '../../core/models/impuesto.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-impuestos-list',
  standalone: true,
  imports: [MatFormFieldModule, MatInputModule, MatSelectModule, ConsultaRegistroDirective, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule],
  templateUrl: './impuestos-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './impuestos-list.component.scss'
})
export class ImpuestosListComponent implements OnInit {
  private readonly acciones = crearAccionesInstitucionales('Impuestos');
  readonly cambiandoEstado = signal(false);
  readonly buscar = signal('');
  readonly estadoFiltro = signal<'todos' | 'activos' | 'inactivos'>('todos');
  readonly visibles = computed(() => {
    const termino = this.buscar().trim().toLocaleLowerCase('es-HN');
    return this.impuestos().filter(item => (!termino || item.nombre.toLocaleLowerCase('es-HN').includes(termino)) &&
      (this.estadoFiltro() === 'todos' || item.activo === (this.estadoFiltro() === 'activos')));
  });
  limpiarFiltros(): void { this.buscar.set(''); this.estadoFiltro.set('todos'); }

  async abrirEditor(id?: number): Promise<void> {
    if (await this.acciones.abrir(() => import('./impuesto-form.component').then(m => m.ImpuestoFormComponent), 'l', id ? 'Editar' : 'Crear', id)) this.cargar();
  }

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
    private snackBar: MatSnackBar
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

  async toggleActivo(impuesto: Impuesto, evento: MatSlideToggleChange): Promise<void> {
    evento.source.checked = impuesto.activo;
    if (!this.puedeCambiarEstado(impuesto) || this.cambiandoEstado()) return;
    this.cambiandoEstado.set(true);
    try {
      const accion = impuesto.activo ? 'Desactivar' : 'Activar';
      if (!await this.acciones.confirmar(accion, { titulo: accion + ' registro', mensaje: '¿Deseas ' + accion.toLowerCase() + ' «' + impuesto.nombre + '»?', confirmarTexto: accion })) return;
      await firstValueFrom(impuesto.activo ? this.service.desactivar(impuesto.id) : this.service.activar(impuesto.id));
      this.cargar();
    } catch { this.snackBar.open('No se pudo cambiar el estado.', 'Cerrar', { duration: 5000 }); }
    finally { this.cambiandoEstado.set(false); }
  }

  async eliminar(impuesto: Impuesto): Promise<void> {
    if (!this.puedeEliminar()) return;

    const confirmado = await this.acciones.confirmar('EliminarLogico', {
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
    const contexto = this.acciones.contexto();
    if (!this.acciones.vigente(contexto, 'Duplicar')) return;

    const nuevoNombre = await this.acciones.solicitarTexto('Duplicar', {
      titulo: 'Duplicar impuesto',
      mensaje: `Define el nombre de la copia de "${impuesto.nombre}".`,
      confirmarTexto: 'Continuar',
      entrada: { etiqueta: 'Nombre del impuesto', valor: `${impuesto.nombre} (copia)`, requerida: true }
    });
    if (!nuevoNombre || !this.acciones.vigente(contexto, 'Duplicar')) return;

    const nuevoCodigo = await this.acciones.solicitarTexto('Duplicar', {
      titulo: 'Código del impuesto',
      mensaje: 'El código debe ser único en el sistema.',
      confirmarTexto: 'Crear copia',
      entrada: { etiqueta: 'Código único', valor: `${impuesto.codigo}-COPIA`, requerida: true }
    });
    if (!nuevoCodigo || !this.acciones.vigente(contexto, 'Duplicar')) return;

    this.service.duplicar(impuesto.id, nuevoNombre, nuevoCodigo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo duplicar.', 'Cerrar', { duration: 5000 })
    });
  }
}
