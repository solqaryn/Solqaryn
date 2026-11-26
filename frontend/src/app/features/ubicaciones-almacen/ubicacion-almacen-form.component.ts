import { MatDialogModule } from '@angular/material/dialog';
import { crearEditorInstitucional } from '../../shared/modal-institucional/acciones-institucionales';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin } from 'rxjs';
import { TipoUbicacionAlmacenOpcion, UbicacionAlmacen, UbicacionAlmacenFormValue } from '../../core/models/ubicacion-almacen.model';
import { AlmacenService } from '../../services/almacen.service';
import { UbicacionAlmacenService } from '../../services/ubicacion-almacen.service';

interface AlmacenOpcion {
  id: number;
  codigo: string;
  nombre: string;
  activo: boolean;
}

interface PadreOpcion {
  id: number;
  codigo: string;
  nombre: string;
  activa: boolean;
}

@Component({
  selector: 'app-ubicacion-almacen-form',
  standalone: true,
  imports: [MatDialogModule, 
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
],
  templateUrl: './ubicacion-almacen-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './ubicacion-almacen-form.component.scss'
})
export class UbicacionAlmacenFormComponent implements OnInit {
  readonly editor = crearEditorInstitucional('UbicacionesAlmacen');
  cancelarModal(): void { if (!this.saving()) this.editor.cerrar(false); }

  private readonly fb = inject(FormBuilder);
  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly loadingPadres = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly almacenes = signal<AlmacenOpcion[]>([]);
  readonly padres = signal<PadreOpcion[]>([]);
  readonly tipos = signal<TipoUbicacionAlmacenOpcion[]>([]);

  private ubicacionId: number | null = null;
  private ubicacionActual: UbicacionAlmacen | null = null;

  readonly form = this.fb.group({
    almacenId: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
    ubicacionPadreId: this.fb.control<number | null>(null),
    codigo: ['', [Validators.required, Validators.maxLength(60)]],
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    tipo: ['', [Validators.required, Validators.maxLength(30)]]
  });

  constructor(
    private ubicacionService: UbicacionAlmacenService,
    private almacenService: AlmacenService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const idParam = this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id');
    if (this.editor.ref && !this.editor.valido(idParam ? 'Editar' : 'Crear')) { this.editor.cerrar(false); return; }
    if (!idParam) {
      this.cargarNuevo();
      return;
    }

    const id = Number(idParam);
    if (!Number.isInteger(id) || id <= 0) {
      this.loading.set(false);
      this.errorMessage.set('El identificador de la ubicación no es válido.');
      this.form.disable();
      return;
    }

    this.isEdit.set(true);
    this.ubicacionId = id;
    this.cargarEdicion(id);
  }

  onAlmacenSeleccionado(): void {
    const almacenId = Number(this.form.controls.almacenId.value);
    this.form.controls.ubicacionPadreId.setValue(null);
    this.padres.set([]);
    if (almacenId > 0) this.cargarPadres(almacenId);
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.form.disabled || this.saving() || this.loading()) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();
    const value: UbicacionAlmacenFormValue = {
      almacenId: Number(raw.almacenId),
      ubicacionPadreId: raw.ubicacionPadreId ? Number(raw.ubicacionPadreId) : null,
      codigo: raw.codigo?.trim() ?? '',
      nombre: raw.nombre?.trim() ?? '',
      tipo: raw.tipo?.trim() ?? ''
    };

    const accion = this.isEdit() ? 'Editar' : 'Crear';
    if (!await this.editor.confirmar(accion, { titulo: 'Confirmar ' + (this.isEdit() ? 'actualización' : 'creación'), mensaje: '¿Deseas guardar los datos del registro?', confirmarTexto: 'Guardar' })) { this.saving.set(false); return; }
    const request$ = this.isEdit()
      ? this.ubicacionService.update(this.ubicacionId!, value)
      : this.ubicacionService.create(value);

    request$.pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/ubicaciones-almacen']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la ubicación.');
      }
    });
  }

  private cargarNuevo(): void {
    this.loading.set(true);
    this.form.disable();

    forkJoin({
      almacenes: this.almacenService.getActivos(),
      tipos: this.ubicacionService.getTipos()
    }).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: ({ almacenes, tipos }) => {
        this.almacenes.set(almacenes.data.map(a => ({ id: a.id, codigo: a.codigo, nombre: a.nombre, activo: true })));
        this.tipos.set(tipos.data);
        this.form.enable();
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudieron cargar los almacenes y tipos disponibles.');
      }
    });
  }

  private cargarEdicion(id: number): void {
    this.loading.set(true);
    this.form.disable();

    forkJoin({
      ubicacion: this.ubicacionService.getById(id),
      almacenes: this.almacenService.getActivos(),
      tipos: this.ubicacionService.getTipos()
    }).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: ({ ubicacion, almacenes, tipos }) => {
        const actual = ubicacion.data;
        this.ubicacionActual = actual;
        const opciones: AlmacenOpcion[] = almacenes.data.map(a => ({ id: a.id, codigo: a.codigo, nombre: a.nombre, activo: true }));
        if (!opciones.some(a => a.id === actual.almacenId)) {
          opciones.unshift({ id: actual.almacenId, codigo: actual.almacenCodigo, nombre: actual.almacenNombre, activo: false });
        }

        this.almacenes.set(opciones);
        this.tipos.set(tipos.data);
        this.form.patchValue({
          almacenId: actual.almacenId,
          ubicacionPadreId: actual.ubicacionPadreId ?? null,
          codigo: actual.codigo,
          nombre: actual.nombre,
          tipo: actual.tipo
        });
        this.cargarPadres(actual.almacenId, actual);
        this.form.enable();
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar la ubicación.');
      }
    });
  }

  private cargarPadres(almacenId: number, actual: UbicacionAlmacen | null = this.ubicacionActual): void {
    this.loadingPadres.set(true);
    this.ubicacionService.getActivas(almacenId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => {
        const opciones: PadreOpcion[] = res.data
          .filter(p => p.id !== this.ubicacionId)
          .map(p => ({ id: p.id, codigo: p.codigo, nombre: p.nombre, activa: true }));

        if (actual?.ubicacionPadreId && actual.almacenId === almacenId && !opciones.some(p => p.id === actual.ubicacionPadreId)) {
          opciones.unshift({
            id: actual.ubicacionPadreId,
            codigo: actual.ubicacionPadreCodigo ?? 'HIST',
            nombre: actual.ubicacionPadreNombre ?? 'Ubicación histórica',
            activa: false
          });
        }

        this.padres.set(opciones);
        this.loadingPadres.set(false);
      },
      error: () => {
        this.padres.set([]);
        this.loadingPadres.set(false);
      }
    });
  }
}
