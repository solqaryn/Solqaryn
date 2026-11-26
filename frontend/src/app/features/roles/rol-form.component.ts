import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, OnInit, DestroyRef, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { RolService } from '../../services/rol.service';

@Component({
  selector: 'app-rol-form',
  standalone: true,
  imports: [MatDialogModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule
],
  templateUrl: './rol-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './rol-form.component.scss'
})
export class RolFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly alerts = inject(AppAlertService);
  private readonly destroyRef = inject(DestroyRef);
  readonly modalRef = inject(MatDialogRef<RolFormComponent>, { optional: true });
  private readonly modalData = inject<{ id?: number }>(MAT_DIALOG_DATA, { optional: true });
  private readonly contextoModal = this.tenant.contextoVerificado();
  readonly loading = signal(false);
  cancelarModal(): void { if (!this.saving()) this.modalRef?.close(false); }
  readonly isEdit = signal(false);
  readonly esSistema = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  private rolId: number | null = null;

  form = this.fb.group({
    nombre: ['', Validators.required],
    descripcion: [''],
    esAdministrador: [false]
  });

  constructor(private rolService: RolService, private route: ActivatedRoute, private router: Router) {
    if (this.modalRef) effect(() => { if (!this.contextoModal || this.tenant.contextoVerificado() !== this.contextoModal) this.modalRef?.close(false); });
  }

  ngOnInit(): void {
    const idParam = this.modalRef ? this.modalData?.id : this.route.snapshot.paramMap.get('id');
    if (this.modalRef && (!this.contextoModal || !this.permisos.puede('Roles', idParam ? 'Editar' : 'Crear'))) { this.modalRef.close(false); return; }
    if (idParam) {
      this.isEdit.set(true);
      this.rolId = Number(idParam);
      this.loading.set(true);
      this.rolService.getById(this.rolId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: (res) => {
        this.loading.set(false);
        this.esSistema.set(res.data.esSistema);
        this.form.patchValue({
          nombre: res.data.nombre,
          descripcion: res.data.descripcion,
          esAdministrador: res.data.esAdministrador
        });
        // EsAdministrador no se puede cambiar después de creado (evita que un rol
        // gane/pierda acceso total accidentalmente por edición).
        this.form.get('esAdministrador')?.disable();
      }, error: () => { this.loading.set(false); this.form.disable(); this.errorMessage.set('No se pudo cargar el rol.'); } });
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.form.disabled || this.loading() || this.saving()) return;

    this.saving.set(true);
    const contexto = this.tenant.contextoVerificado();
    const confirmado = await this.alerts.confirmar({ titulo: this.isEdit() ? 'Confirmar actualización' : 'Confirmar creación', mensaje: this.form.value.esAdministrador && !this.isEdit() ? '¿Deseas crear un rol con acceso total de administrador?' : '¿Deseas guardar los datos del rol?', confirmarTexto: 'Guardar' }).catch(() => false);
    if (!confirmado || this.destroyRef.destroyed || !contexto || contexto !== this.tenant.contextoVerificado() || !this.permisos.puede('Roles', this.isEdit() ? 'Editar' : 'Crear')) { this.saving.set(false); return; }
    this.errorMessage.set(null);

    const nombre = this.form.value.nombre!;
    const descripcion = this.form.value.descripcion || undefined;

    const request$ = this.isEdit()
      ? this.rolService.update(this.rolId!, { nombre, descripcion })
      : this.rolService.create({ nombre, descripcion, esAdministrador: !!this.form.value.esAdministrador });

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        if (this.modalRef) this.modalRef.close(true);
        else void this.router.navigate(['/roles']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el rol.');
      }
    });
  }
}
