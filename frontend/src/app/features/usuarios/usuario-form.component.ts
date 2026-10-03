
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsuarioService } from '../../services/usuario.service';
import { RolService } from '../../services/rol.service';
import { Rol } from '../../core/models/rol.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

const USUARIO_VALIDO = /^[a-zA-Z0-9._-]{3,50}$/;
const PASSWORD_SEGURA = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{10,128}$/;

@Component({
  selector: 'app-usuario-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
],
  templateUrl: './usuario-form.component.html',
  styleUrl: './usuario-form.component.scss'
})
export class UsuarioFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly roles = signal<Rol[]>([]);
  readonly usuarioOriginal = signal('');
  readonly rolActualNombre = signal('—');
  readonly puedeAsignarRol = signal(false);
  readonly puedeRestablecerPassword = signal(false);

  private usuarioId = 0;
  private rolIdOriginal: number | undefined;
  private rolLegadoOriginal = 'Vendedor';

  readonly form = this.fb.group({
    nombreUsuario: ['', [Validators.required, Validators.pattern(USUARIO_VALIDO)]],
    nombreCompleto: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    rolId: [null as number | null, Validators.required],
    nuevaPassword: ['', Validators.pattern(PASSWORD_SEGURA)]
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private usuarioService: UsuarioService,
    private rolService: RolService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeAsignarRol.set(this.permisosRuntime.puede('Usuarios', 'AsignarRol'));
    this.puedeRestablecerPassword.set(
      this.permisosRuntime.puede('Usuarios', 'RestablecerContrasena')
    );

    if (!this.puedeAsignarRol()) this.form.controls.rolId.disable();
    if (!this.puedeRestablecerPassword()) this.form.controls.nuevaPassword.disable();

    this.usuarioId = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(this.usuarioId) || this.usuarioId <= 0) {
      this.router.navigate(['/usuarios']);
      return;
    }

    this.usuarioService.getById(this.usuarioId).subscribe({
      next: (usuario) => {
        const u = usuario.data;
        this.usuarioOriginal.set(u.nombreUsuario);
        this.rolIdOriginal = u.rolId;
        this.rolLegadoOriginal = u.rol;
        this.rolActualNombre.set(u.rolNombre || u.rol);
        this.form.patchValue({
          nombreUsuario: u.nombreUsuario,
          nombreCompleto: u.nombreCompleto,
          rolId: u.rolId ?? null,
          nuevaPassword: ''
        });

        if (this.puedeAsignarRol()) this.cargarRoles(u.rolId);
        else this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo cargar el usuario.');
      }
    });
  }

  private cargarRoles(rolActualId?: number): void {
    this.rolService.getAll(false).subscribe({
      next: (roles) => {
        this.roles.set(roles.data.filter((rol) => rol.activo || rol.id === rolActualId));
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudieron cargar los roles asignables.');
      }
    });
  }

  guardar(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const valor = this.form.getRawValue();
    let rolId = this.rolIdOriginal;
    let rolLegado = this.rolLegadoOriginal;

    if (this.puedeAsignarRol()) {
      const rol = this.roles().find((item) => item.id === valor.rolId);
      if (!rol) {
        this.errorMessage.set('Selecciona un rol activo.');
        return;
      }
      rolId = rol.id;
      rolLegado = rol.esAdministrador ? 'Administrador' : 'Vendedor';
    }

    this.saving.set(true);
    this.errorMessage.set(null);
    this.usuarioService.update(this.usuarioId, {
      nombreUsuario: valor.nombreUsuario!.trim(),
      nombreCompleto: valor.nombreCompleto!.trim(),
      rol: rolLegado,
      rolId,
      nuevaPassword: this.puedeRestablecerPassword()
        ? valor.nuevaPassword?.trim() || undefined
        : undefined
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Usuario actualizado correctamente.', 'Cerrar', { duration: 4000 });
        this.router.navigate(['/usuarios', this.usuarioId]);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo actualizar el usuario.');
      }
    });
  }
}
