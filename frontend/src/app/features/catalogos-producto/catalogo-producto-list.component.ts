
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { CatalogoProducto, TipoCatalogoProducto } from '../../core/models/catalogo-producto.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-catalogo-producto-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule
],
  templateUrl: './catalogo-producto-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './catalogo-producto-list.component.scss'
})
export class CatalogoProductoListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(CatalogoProductoService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly alerts = inject(AppAlertService);

  readonly tipo = this.route.snapshot.data['tipo'] as TipoCatalogoProducto;
  readonly modulo = this.route.snapshot.data['modulo'] as string;
  readonly titulo = this.route.snapshot.data['titulo'] as string;
  readonly singular = this.route.snapshot.data['singular'] as string;
  readonly icono = this.route.snapshot.data['icono'] as string;

  readonly elementos = signal<CatalogoProducto[]>([]);
  readonly marcas = signal<CatalogoProducto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly mostrandoFormulario = signal(false);
  readonly editandoId = signal<number | null>(null);
  readonly buscar = signal('');

  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);

  readonly form = this.fb.group({
    nombre: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(120)]],
    descripcion: ['', Validators.maxLength(500)],
    codigoVisual: ['#1D4ED8', Validators.pattern(/^#[0-9A-Fa-f]{6}$/)],
    orden: [0, [Validators.required, Validators.min(0)]],
    catalogoPadreId: [null as number | null]
  });

  get esColor(): boolean { return this.tipo === 'Color'; }
  get esModelo(): boolean { return this.tipo === 'Modelo'; }

  ngOnInit(): void {
    this.puedeCrear.set(this.permisos.puede(this.modulo, 'Crear'));
    this.puedeEditar.set(this.permisos.puede(this.modulo, 'Editar'));
    this.puedeActivar.set(this.permisos.puede(this.modulo, 'Activar'));
    this.puedeDesactivar.set(this.permisos.puede(this.modulo, 'Desactivar'));
    this.puedeEliminar.set(this.permisos.puede(this.modulo, 'EliminarLogico'));

    if (this.esModelo) {
      this.form.controls.catalogoPadreId.addValidators(Validators.required);
      this.service.getActivos('Marca').subscribe({
        next: (res) => this.marcas.set(res.data),
        error: () => this.mostrarError('No se pudieron cargar las marcas.')
      });
    }

    if (!this.esColor) this.form.controls.codigoVisual.disable();
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.service.getAll(this.tipo, this.buscar()).subscribe({
      next: (res) => {
        this.elementos.set(res.data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.mostrarError(`No se pudieron cargar ${this.titulo.toLowerCase()}.`);
      }
    });
  }

  buscarAhora(event: Event): void {
    this.buscar.set((event.target as HTMLInputElement).value);
    this.cargar();
  }

  nuevo(): void {
    this.editandoId.set(null);
    this.form.reset({
      nombre: '',
      descripcion: '',
      codigoVisual: '#1D4ED8',
      orden: 0,
      catalogoPadreId: null
    });
    if (!this.esColor) this.form.controls.codigoVisual.disable();
    this.mostrandoFormulario.set(true);
  }

  editar(elemento: CatalogoProducto): void {
    this.editandoId.set(elemento.id);
    this.form.patchValue({
      nombre: elemento.nombre,
      descripcion: elemento.descripcion ?? '',
      codigoVisual: elemento.codigoVisual ?? '#1D4ED8',
      orden: elemento.orden,
      catalogoPadreId: elemento.catalogoPadreId ?? null
    });
    this.mostrandoFormulario.set(true);
  }

  cancelar(): void {
    this.mostrandoFormulario.set(false);
    this.editandoId.set(null);
  }

  guardar(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const raw = this.form.getRawValue();
    const value = {
      nombre: raw.nombre!.trim(),
      descripcion: raw.descripcion?.trim() || undefined,
      codigoVisual: this.esColor ? raw.codigoVisual?.toUpperCase() : undefined,
      orden: Number(raw.orden ?? 0),
      catalogoPadreId: this.esModelo ? raw.catalogoPadreId : null
    };

    const id = this.editandoId();
    const request$ = id
      ? this.service.update(this.tipo, id, value)
      : this.service.create(this.tipo, value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelar();
        this.snackBar.open(`${this.singular} guardado correctamente.`, 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.mostrarError(err.error?.message ?? `No se pudo guardar ${this.singular.toLowerCase()}.`);
      }
    });
  }

  puedeCambiarEstado(elemento: CatalogoProducto): boolean {
    return elemento.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  cambiarEstado(elemento: CatalogoProducto): void {
    if (!this.puedeCambiarEstado(elemento)) return;
    const request$ = elemento.activo
      ? this.service.desactivar(this.tipo, elemento.id)
      : this.service.activar(this.tipo, elemento.id);

    request$.subscribe({
      next: (res) => {
        this.elementos.update(items => items.map(item => item.id === elemento.id ? res.data : item));
      },
      error: (err) => this.mostrarError(err.error?.message ?? 'No se pudo cambiar el estado.')
    });
  }

  async eliminar(elemento: CatalogoProducto): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: `Eliminar ${this.singular.toLowerCase()}`,
      mensaje: `Se ocultará “${elemento.nombre}” sin borrar el historial relacionado.`,
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;

    this.service.delete(this.tipo, elemento.id).subscribe({
      next: () => {
        this.elementos.update(items => items.filter(item => item.id !== elemento.id));
        this.snackBar.open(`${this.singular} eliminado correctamente.`, 'Cerrar', { duration: 3500 });
      },
      error: (err) => this.mostrarError(err.error?.message ?? `No se pudo eliminar ${this.singular.toLowerCase()}.`)
    });
  }

  private mostrarError(mensaje: string): void {
    this.snackBar.open(mensaje, 'Cerrar', { duration: 5000 });
  }
}
