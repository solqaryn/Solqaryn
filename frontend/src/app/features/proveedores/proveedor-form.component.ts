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
  imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './proveedor-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './proveedor-form.component.scss'
})
export class ProveedorFormComponent implements OnInit {
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

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.proveedorId = Number(idParam);
      this.proveedorService.getById(this.proveedorId).subscribe((res) => {
        this.form.patchValue(res.data);
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = { ...this.form.getRawValue(), activo: true } as any;
    const request$ = this.isEdit()
      ? this.proveedorService.update(this.proveedorId!, { ...value, activo: true })
      : this.proveedorService.create(value);

    request$.subscribe({
      next: () => { this.saving.set(false); this.router.navigate(['/proveedores']); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el proveedor.'); }
    });
  }
}
