import { MatDialogModule } from '@angular/material/dialog';
import { crearEditorInstitucional } from '../../shared/modal-institucional/acciones-institucionales';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UsuarioService } from '../../services/usuario.service';
import { UsuarioDetalle } from '../../core/models/usuario.model';

@Component({
  selector: 'app-usuario-detail',
  standalone: true,
  imports: [MatDialogModule,CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './usuario-detail.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './usuario-detail.component.scss'
})
export class UsuarioDetailComponent implements OnInit {
  readonly editor = crearEditorInstitucional('Usuarios');
  readonly usuario = signal<UsuarioDetalle | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);

  constructor(private usuarioService: UsuarioService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    const id = Number(this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id'));
    if (this.editor.ref && (!this.editor.valido('Ver') || !Number.isInteger(id) || id <= 0)) { this.editor.cerrar(false); return; }
    this.usuarioService.getById(id).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => {
        this.usuario.set(res.data);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }
}
