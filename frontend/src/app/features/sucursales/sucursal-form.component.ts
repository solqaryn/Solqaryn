import { MatDialogModule } from '@angular/material/dialog';
import { crearEditorInstitucional } from '../../shared/modal-institucional/acciones-institucionales';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import { SucursalFormValue } from '../../core/models/sucursal.model';
import { SucursalService } from '../../services/sucursal.service';

interface EmpresaOpcion {
  id: number;
  nombre: string;
  activa: boolean;
}

@Component({
  selector: 'app-sucursal-form',
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
  templateUrl: './sucursal-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './sucursal-form.component.scss'
})
export class SucursalFormComponent implements OnInit {
  readonly editor = crearEditorInstitucional('Sucursales');
  cancelarModal(): void { if (!this.saving()) this.editor.cerrar(false); }

  private readonly fb = inject(FormBuilder);
  readonly isEdit = signal(false);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly empresas = signal<EmpresaOpcion[]>([]);
  readonly empresasLoading = signal(true);
  readonly empresasError = signal<string | null>(null);
  readonly zonasSugeridas = [
    'America/Tegucigalpa',
    'America/Guatemala',
    'America/El_Salvador',
    'America/Managua',
    'America/Costa_Rica',
    'America/Panama',
    'America/Mexico_City',
    'America/New_York'
  ];

  private readonly empresasUrl = `${environment.apiUrl}/empresas`;
  private sucursalId: number | null = null;

  readonly form = this.fb.group({
    empresaId: this.fb.control<number | null>(null, [Validators.required, Validators.min(1)]),
    codigo: ['', [Validators.required, Validators.maxLength(40)]],
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    direccion: ['', [Validators.maxLength(500)]],
    telefono: ['', [Validators.maxLength(50)]],
    correo: ['', [Validators.email, Validators.maxLength(254)]],
    zonaHoraria: ['America/Tegucigalpa', [Validators.required, Validators.maxLength(100)]]
  });

  constructor(
    private sucursalService: SucursalService,
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.cargarEmpresas();

    const idParam = this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id');
    if (this.editor.ref && !this.editor.valido(idParam ? 'Editar' : 'Crear')) { this.editor.cerrar(false); return; }
    if (!idParam) return;

    const id = Number(idParam);
    if (!Number.isInteger(id) || id <= 0) {
      this.errorMessage.set('El identificador de la sucursal no es válido.');
      this.form.disable();
      return;
    }

    this.isEdit.set(true);
    this.sucursalId = id;
    this.cargarSucursal(id);
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.form.disabled || this.saving() || this.loading() || this.empresasLoading() || this.empresasError()) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();
    const empresaId = Number(raw.empresaId);
    if (!Number.isInteger(empresaId) || empresaId <= 0) {
      this.saving.set(false);
      this.form.controls.empresaId.setErrors({ tenantOwnerRequired: true });
      return;
    }

    const value: SucursalFormValue = {
      empresaId,
      codigo: raw.codigo?.trim() ?? '',
      nombre: raw.nombre?.trim() ?? '',
      direccion: this.opcional(raw.direccion),
      telefono: this.opcional(raw.telefono),
      correo: this.opcional(raw.correo),
      zonaHoraria: raw.zonaHoraria?.trim() || 'America/Tegucigalpa'
    };

    const accion = this.isEdit() ? 'Editar' : 'Crear';
    if (!await this.editor.confirmar(accion, { titulo: 'Confirmar ' + (this.isEdit() ? 'actualización' : 'creación'), mensaje: '¿Deseas guardar los datos del registro?', confirmarTexto: 'Guardar' })) { this.saving.set(false); return; }
    const request$ = this.isEdit()
      ? this.sucursalService.update(this.sucursalId!, value)
      : this.sucursalService.create(value);

    request$.pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/sucursales']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la sucursal.');
      }
    });
  }

  private cargarEmpresas(): void {
    this.empresasLoading.set(true);
    this.empresasError.set(null);
    this.http.get<ApiResponse<EmpresaOpcion[]>>(this.empresasUrl).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => {
        this.empresas.set(res.data ?? []);
        this.empresasLoading.set(false);
      },
      error: () => {
        this.empresas.set([]);
        this.empresasLoading.set(false);
        this.empresasError.set('No se pudieron cargar las empresas. Reintenta antes de guardar.');
        this.form.controls.empresaId.disable();
      }
    });
  }

  private cargarSucursal(id: number): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.form.disable();

    this.sucursalService.getById(id).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => {
        const sucursal = res.data;
        this.form.patchValue({
          empresaId: sucursal.empresaId ?? null,
          codigo: sucursal.codigo,
          nombre: sucursal.nombre,
          direccion: sucursal.direccion ?? '',
          telefono: sucursal.telefono ?? '',
          correo: sucursal.correo ?? '',
          zonaHoraria: sucursal.zonaHoraria
        });
        this.form.enable();
        if (this.empresasError()) this.form.controls.empresaId.disable();
        if (!sucursal.empresaId) {
          this.form.controls.empresaId.markAsTouched();
          this.errorMessage.set('Esta sucursal proviene del rollout legado y todavía no tiene una empresa propietaria válida. Selecciona un Empresa ID antes de guardar.');
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar la sucursal.');
      }
    });
  }

  private opcional(value: string | null | undefined): string | null {
    const limpio = value?.trim();
    return limpio ? limpio : null;
  }
}
