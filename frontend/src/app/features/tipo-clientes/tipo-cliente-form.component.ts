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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { TipoClienteService } from '../../services/tipo-cliente.service';
import { TipoCliente } from '../../core/models/tipo-cliente.model';

@Component({
  selector: 'app-tipo-cliente-form',
  standalone: true,
  imports: [MatDialogModule, 
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule
],
  templateUrl: './tipo-cliente-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './tipo-cliente-form.component.scss'
})
export class TipoClienteFormComponent implements OnInit {
  readonly editor = crearEditorInstitucional('TiposClientes');
  readonly loading = signal(false);
  cancelarModal(): void { if (!this.saving()) this.editor.cerrar(false); }

  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly esSistema = signal(false);
  readonly codigo = signal<string | null>(null);
  private tipoId: number | null = null;

  form!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private tipoClienteService: TipoClienteService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      nombre: ['', [Validators.required, Validators.maxLength(100)]],
      descripcion: ['', Validators.maxLength(500)],
      colorHex: ['#9E9E9E', [Validators.required, Validators.pattern(/^#[0-9A-Fa-f]{6}$/)]],
      orden: [0, [Validators.required, Validators.min(0)]],
      esPredeterminado: [false],
      activo: [true]
    });

    const idParam = this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id');
    if (this.editor.ref && !this.editor.valido(idParam ? 'Editar' : 'Crear')) { this.editor.cerrar(false); return; }
    if (idParam) {
      this.isEdit.set(true);
      this.tipoId = Number(idParam);
      this.loading.set(true);
      this.tipoClienteService.getById(this.tipoId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({ next: (res) => {
        this.loading.set(false);
        this.form.patchValue(res.data);
        this.esSistema.set(res.data.esSistema);
        this.codigo.set(res.data.codigo);

        if (res.data.codigo === 'SIN_CLASIFICAR') {
          this.form.get('activo')?.disable();
          this.form.get('esPredeterminado')?.disable();
        }
      }, error: () => { this.loading.set(false); this.form.disable(); this.errorMessage.set('No se pudo cargar el registro.'); } });
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.form.disabled || this.loading() || this.saving()) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    const accion = this.isEdit() ? 'Editar' : 'Crear';
    if (!await this.editor.confirmar(accion, { titulo: 'Confirmar ' + (this.isEdit() ? 'actualización' : 'creación'), mensaje: '¿Deseas guardar los datos del registro?', confirmarTexto: 'Guardar' })) { this.saving.set(false); return; }
    const request$ = this.isEdit()
      ? this.tipoClienteService.update(this.tipoId!, value)
      : this.tipoClienteService.create(value);

    request$.pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/tipo-clientes']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la clasificación.');
      }
    });
  }
}
