import { MatDialogModule } from '@angular/material/dialog';
import { crearEditorInstitucional } from '../../shared/modal-institucional/acciones-institucionales';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProveedorService } from '../../services/proveedor.service';

@Component({
  selector: 'app-proveedor-form',
  standalone: true,
  imports: [MatDialogModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './proveedor-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './proveedor-form.component.scss'
})
export class ProveedorFormComponent implements OnInit {
  readonly editor = crearEditorInstitucional('Proveedores');
  readonly loading = signal(false);
  cancelarModal(): void { if (!this.saving()) this.editor.cerrar(false); }
  private activoOriginal = true;

  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  private proveedorId: number | null = null;

  form!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private proveedorService: ProveedorService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      nombre: ['', Validators.required],
      telefono: [''],
      documento: [''],
      correo: [''],
      direccion: ['']
    });

    const idParam = this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id');
    if (this.editor.ref && !this.editor.valido(idParam ? 'Editar' : 'Crear')) { this.editor.cerrar(false); return; }
    if (idParam) {
      this.isEdit.set(true);
      this.proveedorId = Number(idParam);
      this.loading.set(true);
      this.proveedorService.getById(this.proveedorId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({ next: (res) => {
        this.loading.set(false);
        this.activoOriginal = res.data.activo;
        this.form.patchValue(res.data);
      }, error: () => { this.loading.set(false); this.form.disable(); this.errorMessage.set('No se pudo cargar el registro.'); } });
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.form.disabled || this.loading() || this.saving()) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = { ...this.form.getRawValue(), activo: this.isEdit() ? this.activoOriginal : true } as any;
    const accion = this.isEdit() ? 'Editar' : 'Crear';
    if (!await this.editor.confirmar(accion, { titulo: 'Confirmar ' + (this.isEdit() ? 'actualización' : 'creación'), mensaje: '¿Deseas guardar los datos del registro?', confirmarTexto: 'Guardar' })) { this.saving.set(false); return; }
    const request$ = this.isEdit()
      ? this.proveedorService.update(this.proveedorId!, { ...value, activo: this.isEdit() ? this.activoOriginal : true })
      : this.proveedorService.create(value);

    request$.pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => { this.saving.set(false); if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/proveedores']); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el proveedor.'); }
    });
  }
}
