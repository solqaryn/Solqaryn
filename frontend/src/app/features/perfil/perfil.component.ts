import { Component, OnDestroy, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PerfilService } from '../../services/perfil.service';
import { Perfil } from '../../core/models/perfil.model';
import { AuthService } from '../../core/auth/auth.service';

function passwordsIguales(control: AbstractControl): ValidationErrors | null {
  const nueva = control.get('passwordNueva')?.value;
  const confirmacion = control.get('passwordConfirmacion')?.value;
  return nueva && confirmacion && nueva !== confirmacion ? { noCoincide: true } : null;
}

const PASSWORD_SEGURA = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{10,128}$/;
const USUARIO_VALIDO = /^[a-zA-Z0-9._-]{3,50}$/;
const TIPOS_FOTO = new Set(['image/jpeg', 'image/png', 'image/webp']);
const MAX_FOTO_BYTES = 5 * 1024 * 1024;

@Component({
  selector: 'app-perfil',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './perfil.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './perfil.component.scss'
})
export class PerfilComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private previewObjectUrl: string | null = null;

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly cambiandoPassword = signal(false);
  readonly subiendoFoto = signal(false);
  readonly eliminandoFoto = signal(false);
  readonly perfil = signal<Perfil | null>(null);
  readonly fotoSeleccionada = signal<File | null>(null);
  readonly fotoPreviewUrl = signal<string | null>(null);

  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly errorPassword = signal<string | null>(null);
  readonly successPassword = signal<string | null>(null);
  readonly errorFoto = signal<string | null>(null);

  readonly formPerfil = this.fb.group({
    nombreUsuario: ['', [Validators.required, Validators.pattern(USUARIO_VALIDO)]],
    nombreCompleto: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]]
  });

  readonly formPassword = this.fb.group({
    passwordActual: ['', Validators.required],
    passwordNueva: ['', [Validators.required, Validators.pattern(PASSWORD_SEGURA)]],
    passwordConfirmacion: ['', Validators.required]
  }, { validators: passwordsIguales });

  constructor(
    private readonly perfilService: PerfilService,
    private readonly auth: AuthService,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.cargarPerfil();
  }

  ngOnDestroy(): void {
    this.liberarPreview();
  }

  cargarPerfil(): void {
    this.loading.set(true);
    this.perfilService.get().subscribe({
      next: (res) => {
        this.aplicarPerfil(res.data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar el perfil.');
      }
    });
  }

  guardarPerfil(): void {
    if (this.formPerfil.invalid) {
      this.formPerfil.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const valor = this.formPerfil.getRawValue();
    this.perfilService.update({
      nombreUsuario: valor.nombreUsuario!.trim(),
      nombreCompleto: valor.nombreCompleto!.trim()
    }).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.aplicarPerfil(res.data);
        this.successMessage.set(res.message || 'Perfil actualizado correctamente.');
        this.snackBar.open('Perfil actualizado.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo actualizar el perfil.');
      }
    });
  }

  seleccionarFoto(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0] ?? null;
    this.errorFoto.set(null);
    this.fotoSeleccionada.set(null);
    this.liberarPreview();

    if (!archivo) return;
    if (!TIPOS_FOTO.has(archivo.type)) {
      this.errorFoto.set('La fotografía debe ser JPG, PNG o WebP.');
      input.value = '';
      return;
    }
    if (archivo.size > MAX_FOTO_BYTES) {
      this.errorFoto.set('La fotografía no puede superar 5 MB.');
      input.value = '';
      return;
    }

    this.previewObjectUrl = URL.createObjectURL(archivo);
    this.fotoPreviewUrl.set(this.previewObjectUrl);
    this.fotoSeleccionada.set(archivo);
  }

  subirFoto(): void {
    const foto = this.fotoSeleccionada();
    if (!foto || this.subiendoFoto()) return;

    this.subiendoFoto.set(true);
    this.errorFoto.set(null);
    this.perfilService.actualizarFoto(foto).subscribe({
      next: (res) => {
        this.subiendoFoto.set(false);
        this.fotoSeleccionada.set(null);
        this.liberarPreview();
        this.aplicarPerfil(res.data);
        this.snackBar.open('Fotografía actualizada.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.subiendoFoto.set(false);
        this.errorFoto.set(err.error?.message ?? 'No se pudo actualizar la fotografía.');
      }
    });
  }

  eliminarFoto(): void {
    if (!this.perfil()?.fotoPerfilUrl || this.eliminandoFoto()) return;

    this.eliminandoFoto.set(true);
    this.errorFoto.set(null);
    this.perfilService.eliminarFoto().subscribe({
      next: (res) => {
        this.eliminandoFoto.set(false);
        this.fotoSeleccionada.set(null);
        this.liberarPreview();
        this.aplicarPerfil(res.data);
        this.snackBar.open('Fotografía eliminada.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.eliminandoFoto.set(false);
        this.errorFoto.set(err.error?.message ?? 'No se pudo eliminar la fotografía.');
      }
    });
  }

  cambiarPassword(): void {
    if (this.formPassword.invalid) {
      this.formPassword.markAllAsTouched();
      return;
    }

    this.cambiandoPassword.set(true);
    this.errorPassword.set(null);
    this.successPassword.set(null);

    const valor = this.formPassword.getRawValue();
    this.perfilService.cambiarPassword({
      passwordActual: valor.passwordActual!,
      passwordNueva: valor.passwordNueva!
    }).subscribe({
      next: () => {
        this.cambiandoPassword.set(false);
        this.successPassword.set('Contraseña actualizada correctamente.');
        this.formPassword.reset();
        this.snackBar.open('Contraseña actualizada.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => {
        this.cambiandoPassword.set(false);
        this.errorPassword.set(err.error?.message ?? 'No se pudo cambiar la contraseña.');
      }
    });
  }

  fotoVisible(): string | null {
    return this.fotoPreviewUrl() || this.perfil()?.fotoPerfilUrl || null;
  }

  iniciales(): string {
    const nombre = this.perfil()?.nombreCompleto?.trim() || this.auth.nombreCompleto() || 'Usuario';
    return nombre.split(/\s+/).slice(0, 2).map(parte => parte.charAt(0).toUpperCase()).join('');
  }

  private aplicarPerfil(perfil: Perfil): void {
    this.perfil.set(perfil);
    this.formPerfil.patchValue({
      nombreUsuario: perfil.nombreUsuario,
      nombreCompleto: perfil.nombreCompleto
    });
    this.auth.actualizarIdentidad(perfil);
  }

  private liberarPreview(): void {
    if (this.previewObjectUrl) URL.revokeObjectURL(this.previewObjectUrl);
    this.previewObjectUrl = null;
    this.fotoPreviewUrl.set(null);
  }
}
