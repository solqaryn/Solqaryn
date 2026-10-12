import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';

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
  imports: [
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

  constructor(private rolService: RolService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.rolId = Number(idParam);
      this.rolService.getById(this.rolId).subscribe((res) => {
        this.esSistema.set(res.data.esSistema);
        this.form.patchValue({
          nombre: res.data.nombre,
          descripcion: res.data.descripcion,
          esAdministrador: res.data.esAdministrador
        });
        // EsAdministrador no se puede cambiar después de creado (evita que un rol
        // gane/pierda acceso total accidentalmente por edición).
        this.form.get('esAdministrador')?.disable();
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const nombre = this.form.value.nombre!;
    const descripcion = this.form.value.descripcion || undefined;

    const request$ = this.isEdit()
      ? this.rolService.update(this.rolId!, { nombre, descripcion })
      : this.rolService.create({ nombre, descripcion, esAdministrador: !!this.form.value.esAdministrador });

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/roles']);
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el rol.');
      }
    });
  }
}
