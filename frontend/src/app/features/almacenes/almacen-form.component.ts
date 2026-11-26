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
import { AlmacenFormValue, TipoAlmacenOpcion } from '../../core/models/almacen.model';
import { AlmacenService } from '../../services/almacen.service';
import { SucursalService } from '../../services/sucursal.service';

interface SucursalAlmacenOpcion {
  id: number;
  codigo: string;
  nombre: string;
  activa: boolean;
}

@Component({
  selector: 'app-almacen-form',
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
  templateUrl: './almacen-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './almacen-form.component.scss'
})
export class AlmacenFormComponent implements OnInit {
  readonly editor = crearEditorInstitucional('Almacenes');
  cancelarModal(): void { if (!this.saving()) this.editor.cerrar(false); }

  private readonly fb = inject(FormBuilder);
  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly sucursales = signal<SucursalAlmacenOpcion[]>([]);
  readonly tipos = signal<TipoAlmacenOpcion[]>([]);

  private almacenId: number | null = null;

  readonly form = this.fb.group({
    sucursalId: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
    codigo: ['', [Validators.required, Validators.maxLength(40)]],
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    tipo: ['', [Validators.required, Validators.maxLength(30)]]
  });

  constructor(
    private almacenService: AlmacenService,
    private sucursalService: SucursalService,
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
      this.errorMessage.set('El identificador del almacén no es válido.');
      this.form.disable();
      return;
    }

    this.isEdit.set(true);
    this.almacenId = id;
    this.cargarEdicion(id);
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.form.disabled || this.saving() || this.loading()) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();
    const value: AlmacenFormValue = {
      sucursalId: Number(raw.sucursalId),
      codigo: raw.codigo?.trim() ?? '',
      nombre: raw.nombre?.trim() ?? '',
      tipo: raw.tipo?.trim() ?? ''
    };

    const accion = this.isEdit() ? 'Editar' : 'Crear';
    if (!await this.editor.confirmar(accion, { titulo: 'Confirmar ' + (this.isEdit() ? 'actualización' : 'creación'), mensaje: '¿Deseas guardar los datos del registro?', confirmarTexto: 'Guardar' })) { this.saving.set(false); return; }
    const request$ = this.isEdit()
      ? this.almacenService.update(this.almacenId!, value)
      : this.almacenService.create(value);

    request$.pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/almacenes']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el almacén.');
      }
    });
  }

  private cargarNuevo(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.form.disable();

    forkJoin({
      sucursales: this.sucursalService.getActivas(),
      tipos: this.almacenService.getTipos()
    }).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: ({ sucursales, tipos }) => {
        this.sucursales.set(sucursales.data.map(s => ({ id: s.id, codigo: s.codigo, nombre: s.nombre, activa: true })));
        this.tipos.set(tipos.data);
        this.form.enable();
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudieron cargar las sucursales y tipos disponibles.');
      }
    });
  }

  private cargarEdicion(id: number): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.form.disable();

    forkJoin({
      almacen: this.almacenService.getById(id),
      sucursales: this.sucursalService.getActivas(),
      tipos: this.almacenService.getTipos()
    }).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: ({ almacen, sucursales, tipos }) => {
        const actual = almacen.data;
        const opciones: SucursalAlmacenOpcion[] = sucursales.data.map(s => ({
          id: s.id,
          codigo: s.codigo,
          nombre: s.nombre,
          activa: true
        }));

        if (!opciones.some(s => s.id === actual.sucursalId)) {
          opciones.unshift({
            id: actual.sucursalId,
            codigo: actual.sucursalCodigo,
            nombre: actual.sucursalNombre,
            activa: false
          });
        }

        this.sucursales.set(opciones);
        this.tipos.set(tipos.data);
        this.form.patchValue({
          sucursalId: actual.sucursalId,
          codigo: actual.codigo,
          nombre: actual.nombre,
          tipo: actual.tipo
        });
        this.form.enable();
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar el almacén.');
      }
    });
  }
}
