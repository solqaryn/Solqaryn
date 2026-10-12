import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';

import { HttpErrorResponse } from '@angular/common/http';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/auth/auth.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { SessionActivityService } from '../../core/auth/session-activity.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { TenantSelectorComponent } from '../../core/auth/tenant-selector.component';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TenantSelectorComponent
],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  readonly authService = inject(AuthService);
  readonly tenantContext = inject(TenantContextService);
  private readonly permisosRuntime = inject(PermisosRuntimeService);
  private readonly sessionActivity = inject(SessionActivityService);
  readonly identidad = inject(EmpresaIdentidadService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hidePassword = signal(true);

  readonly form = this.fb.group({
    nombreUsuario: ['', [Validators.required, Validators.maxLength(50)]],
    password: ['', [Validators.required, Validators.maxLength(128)]]
  });

  constructor() {
    this.identidad.usarPlataforma();

    if (this.authService.isAuthenticated()) {
      if (this.tenantContext.tieneContextoVerificado()) {
        this.redirigirSegunPermisos();
      } else {
        // Sesión válida sin tenant verificado: permanecer en el gate de empresa.
        // No cargar permisos ni navegar al ERP hasta validar la membresía server-side.
        this.permisosRuntime.limpiar();
      }
      return;
    }

    const mensaje = this.sessionActivity.tomarMensajePendiente();
    if (mensaje) this.errorMessage.set(mensaje);
  }

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    const valor = this.form.getRawValue();
    this.authService.login({
      nombreUsuario: valor.nombreUsuario!.trim(),
      password: valor.password!
    }).subscribe({
      next: () => {
        const empresaSolicitada = this.tenantContext.empresaSolicitadaId();
        this.sessionActivity.limpiarMensajePendiente();
        this.sessionActivity.iniciar();
        this.permisosRuntime.limpiar();

        // Una empresa persistida sigue siendo sólo una solicitud. Tras autenticar,
        // la revalidamos contra UsuarioEmpresa antes de navegar. Esto conserva la
        // continuidad del tenant sin convertir localStorage en autoridad.
        if (empresaSolicitada) {
          this.tenantContext.seleccionarEmpresa(empresaSolicitada).subscribe({
            next: () => this.redirigirSegunPermisos(),
            error: () => {
              this.loading.set(false);
              // TenantContextService ya revocó la solicitud/contexto inválido.
              // La sesión permanece autenticada y el usuario puede seleccionar
              // manualmente otra empresa autorizada desde el gate.
            }
          });
          return;
        }

        this.tenantContext.limpiar();
        this.loading.set(false);
        // Sin empresa solicitada, el template permanece en el gate de empresa.
        // No existe navegación ERP antes de una confirmación server-side válida.
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage.set(this.obtenerMensajeLogin(err));
      }
    });
  }

  tenantConfirmado(): void {
    if (!this.tenantContext.tieneContextoVerificado()) return;
    this.redirigirSegunPermisos();
  }

  private obtenerMensajeLogin(err: HttpErrorResponse): string {
    const mensajeBackend = typeof err.error?.message === 'string'
      ? err.error.message.trim()
      : '';

    if (mensajeBackend) return mensajeBackend;

    if (err.status === 0) {
      return 'No fue posible conectar con el servidor. Verifica tu conexión e intenta nuevamente.';
    }

    if (err.status === 401) {
      return 'El nombre de usuario o la contraseña son incorrectos.';
    }

    if (err.status === 403) {
      return 'La cuenta no tiene autorización para ingresar al sistema.';
    }

    if (err.status >= 500) {
      return 'El servidor no pudo procesar el inicio de sesión. Intenta nuevamente en unos minutos.';
    }

    return 'No se pudo iniciar sesión. Revisa los datos ingresados e intenta nuevamente.';
  }

  private redirigirSegunPermisos(): void {
    if (!this.tenantContext.tieneContextoVerificado()) {
      this.loading.set(false);
      this.permisosRuntime.limpiar();
      return;
    }

    this.loading.set(true);
    this.permisosRuntime.cargar().subscribe({
      next: () => {
        this.loading.set(false);
        const ruta = this.permisosRuntime.rutaInicialPermitida() ?? '/perfil';
        this.router.navigateByUrl(ruta, { replaceUrl: true });
      },
      error: () => {
        this.loading.set(false);
        this.router.navigateByUrl('/perfil', { replaceUrl: true });
      }
    });
  }
}
