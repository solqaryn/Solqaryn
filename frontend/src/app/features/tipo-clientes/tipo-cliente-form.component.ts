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
  imports: [
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

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.tipoId = Number(idParam);
      this.tipoClienteService.getById(this.tipoId).subscribe((res) => {
        this.form.patchValue(res.data);
        this.esSistema.set(res.data.esSistema);
        this.codigo.set(res.data.codigo);

        if (res.data.codigo === 'SIN_CLASIFICAR') {
          this.form.get('activo')?.disable();
          this.form.get('esPredeterminado')?.disable();
        }
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    const request$ = this.isEdit()
      ? this.tipoClienteService.update(this.tipoId!, value)
      : this.tipoClienteService.create(value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/tipo-clientes']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la clasificación.');
      }
    });
  }
}
