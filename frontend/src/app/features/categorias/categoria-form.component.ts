import { Component, OnInit, inject, signal } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CategoriaService } from '../../services/categoria.service';

@Component({
  selector: 'app-categoria-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  templateUrl: './categoria-form.component.html',
  styleUrl: './categoria-form.component.scss'
})
export class CategoriaFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  private categoriaId: number | null = null;

  form = this.fb.group({
    nombre: ['', Validators.required],
    descripcion: ['']
  });

  constructor(
    private categoriaService: CategoriaService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.categoriaId = Number(idParam);
      this.categoriaService.getById(this.categoriaId).subscribe((res) => {
        this.form.patchValue({ nombre: res.data.nombre, descripcion: res.data.descripcion });
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = { nombre: this.form.value.nombre!, descripcion: this.form.value.descripcion || undefined, activa: true };
    const request$ = this.isEdit()
      ? this.categoriaService.update(this.categoriaId!, value)
      : this.categoriaService.create(value);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/categorias']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la categoría.');
      }
    });
  }
}
