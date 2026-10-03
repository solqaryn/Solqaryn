import { CommonModule } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ApiResponse } from '../../core/models/api-response.model';

interface EmpresaRaiz {
  id: number;
  nombre: string;
  activa: boolean;
  fechaCreacion: string;
  fechaActualizacion: string;
}

type EstadoFiltro = 'todas' | 'activas' | 'inactivas';

@Component({
  selector: 'app-empresa-administracion-card',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule
  ],
  template: `
    <section class="card empresa-card" aria-labelledby="empresa-admin-title">
      <div class="header-row">
        <div>
          <h2 id="empresa-admin-title">Empresas</h2>
          <p class="hint">Administra las empresas raíz disponibles en el sistema.</p>
        </div>
        @if (puedeCrear()) {
          <button mat-flat-button color="primary" type="button" (click)="nueva()" [disabled]="saving()">
            <mat-icon>add_business</mat-icon> Nueva empresa
          </button>
        }
      </div>

      <div class="toolbar">
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Estado</mat-label>
          <mat-select [value]="estado()" (selectionChange)="cambiarEstado($event.value)">
            <mat-option value="todas">Todas</mat-option>
            <mat-option value="activas">Activas</mat-option>
            <mat-option value="inactivas">Inactivas</mat-option>
          </mat-select>
        </mat-form-field>
        <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading()">
          <mat-icon>refresh</mat-icon> Actualizar
        </button>
      </div>

      @if (error()) {
        <div class="state error" role="alert">
          <mat-icon>error_outline</mat-icon>
          <span>{{ error() }}</span>
          <button mat-button type="button" (click)="cargar()">Reintentar</button>
        </div>
      } @else if (loading()) {
        <div class="state" role="status" aria-live="polite"><mat-spinner diameter="32"></mat-spinner><span>Cargando empresas…</span></div>
      } @else if (empresas().length === 0) {
        <div class="state" role="status"><mat-icon>domain_disabled</mat-icon><span>No hay empresas para el filtro seleccionado.</span></div>
      } @else {
        <div class="table-wrap">
          <table>
            <thead><tr><th>Nombre</th><th>Estado</th><th>Actualizada</th><th class="actions">Acciones</th></tr></thead>
            <tbody>
              @for (empresa of empresas(); track empresa.id) {
                <tr>
                  <td><strong>{{ empresa.nombre }}</strong></td>
                  <td><span class="status" [class.inactive]="!empresa.activa">{{ empresa.activa ? 'Activa' : 'Inactiva' }}</span></td>
                  <td>{{ empresa.fechaActualizacion | date:'dd/MM/yyyy HH:mm' }}</td>
                  <td class="actions">
                    <button mat-icon-button type="button" (click)="ver(empresa.id)" aria-label="Ver empresa" matTooltip="Ver empresa"><mat-icon>visibility</mat-icon></button>
                    @if (puedeEditar()) {
                      <button mat-icon-button type="button" (click)="editar(empresa)" aria-label="Editar empresa" matTooltip="Editar empresa"><mat-icon>edit</mat-icon></button>
                    }
                    @if (empresa.activa && puedeDesactivar()) {
                      <button mat-icon-button type="button" (click)="cambiarActivo(empresa, false)" [disabled]="operandoId() === empresa.id" aria-label="Desactivar empresa" matTooltip="Desactivar empresa"><mat-icon>toggle_off</mat-icon></button>
                    }
                    @if (!empresa.activa && puedeActivar()) {
                      <button mat-icon-button type="button" (click)="cambiarActivo(empresa, true)" [disabled]="operandoId() === empresa.id" aria-label="Activar empresa" matTooltip="Activar empresa"><mat-icon>toggle_on</mat-icon></button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }

      @if (detalle()) {
        <div class="detail" role="region" aria-label="Detalle de empresa">
          <div><strong>{{ detalle()!.nombre }}</strong><span>{{ detalle()!.activa ? 'Activa' : 'Inactiva' }}</span></div>
          <button mat-icon-button type="button" (click)="detalle.set(null)" aria-label="Cerrar detalle"><mat-icon>close</mat-icon></button>
        </div>
      }

      @if (formVisible()) {
        <form [formGroup]="form" (ngSubmit)="guardar()" class="editor" aria-label="Formulario de empresa">
          <h3>{{ editingId() ? 'Editar empresa' : 'Nueva empresa' }}</h3>
          <mat-form-field appearance="outline">
            <mat-label>Nombre</mat-label>
            <input matInput formControlName="nombre" maxlength="200" autocomplete="organization" />
            <mat-hint align="end">{{ form.controls.nombre.value?.length || 0 }}/200</mat-hint>
            @if (form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio.</mat-error> }
            @if (form.controls.nombre.hasError('whitespace')) { <mat-error>El nombre no puede contener solo espacios.</mat-error> }
          </mat-form-field>
          <div class="editor-actions">
            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">
              @if (saving()) { <mat-spinner diameter="18"></mat-spinner> } @else { <mat-icon>save</mat-icon> }
              {{ editingId() ? 'Guardar cambios' : 'Crear empresa' }}
            </button>
            <button mat-button type="button" (click)="cancelarEdicion()" [disabled]="saving()">Cancelar</button>
          </div>
        </form>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .empresa-card { margin-top: 1.25rem; padding: 1.25rem; }
    .header-row, .toolbar, .editor-actions, .detail { display:flex; align-items:center; gap:1rem; }
    .header-row { justify-content:space-between; flex-wrap:wrap; }
    h2, h3 { margin:0; }
    .hint { margin:.25rem 0 0; opacity:.75; }
    .toolbar { margin:1rem 0; flex-wrap:wrap; }
    .toolbar mat-form-field { min-width:180px; }
    .state { min-height:120px; display:flex; align-items:center; justify-content:center; gap:.75rem; text-align:center; }
    .state.error { color:var(--color-error, #b3261e); flex-wrap:wrap; }
    .table-wrap { overflow-x:auto; }
    table { width:100%; border-collapse:collapse; min-width:620px; }
    th, td { padding:.8rem .65rem; border-bottom:1px solid rgba(0,0,0,.12); text-align:left; }
    th.actions, td.actions { text-align:right; white-space:nowrap; }
    .status { display:inline-block; border-radius:999px; padding:.2rem .55rem; background:rgba(46,125,50,.12); }
    .status.inactive { background:rgba(0,0,0,.08); }
    .detail { justify-content:space-between; margin-top:1rem; padding:.75rem 1rem; border:1px solid rgba(0,0,0,.12); border-radius:8px; }
    .detail div { display:flex; gap:.75rem; flex-wrap:wrap; }
    .editor { margin-top:1rem; padding-top:1rem; border-top:1px solid rgba(0,0,0,.12); display:grid; gap:1rem; }
    .editor mat-form-field { width:min(100%, 520px); }
    @media (max-width: 640px) { .header-row > button { width:100%; } .toolbar { align-items:stretch; } .toolbar mat-form-field, .toolbar button { width:100%; } }
  `]
})
export class EmpresaAdministracionCardComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly apiUrl = `${environment.apiUrl}/empresas`;
  private cargaRequestId = 0;

  readonly empresas = signal<EmpresaRaiz[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly estado = signal<EstadoFiltro>('todas');
  readonly editingId = signal<number | null>(null);
  readonly formVisible = signal(false);
  readonly detalle = signal<EmpresaRaiz | null>(null);
  readonly operandoId = signal<number | null>(null);
  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);

  readonly form = this.fb.group({ nombre: ['', [Validators.required, Validators.maxLength(200), this.noWhitespaceValidator]] });

  ngOnInit(): void {
    this.puedeCrear.set(this.permisos.puede('Configuracion', 'Crear'));
    this.puedeEditar.set(this.permisos.puede('Configuracion', 'Editar'));
    this.puedeActivar.set(this.permisos.puede('Configuracion', 'Activar'));
    this.puedeDesactivar.set(this.permisos.puede('Configuracion', 'Desactivar'));
    this.cargar();
  }

  cargar(): void {
    const requestId = ++this.cargaRequestId;
    this.loading.set(true);
    this.error.set(null);
    let params = new HttpParams();
    if (this.estado() !== 'todas') params = params.set('activa', this.estado() === 'activas');
    this.http.get<ApiResponse<EmpresaRaiz[]>>(this.apiUrl, { params }).pipe(finalize(() => {
      if (requestId === this.cargaRequestId) this.loading.set(false);
    })).subscribe({
      next: res => {
        if (requestId !== this.cargaRequestId) return;
        this.empresas.set(res.data ?? []);
      },
      error: err => {
        if (requestId !== this.cargaRequestId) return;
        this.empresas.set([]);
        this.error.set(err.error?.message ?? 'No se pudieron cargar las empresas.');
      }
    });
  }

  cambiarEstado(value: EstadoFiltro): void { this.estado.set(value); this.cargar(); }

  ver(id: number): void {
    this.error.set(null);
    this.http.get<ApiResponse<EmpresaRaiz>>(`${this.apiUrl}/${id}`).subscribe({
      next: res => this.detalle.set(res.data),
      error: err => this.error.set(err.error?.message ?? 'No se pudo cargar el detalle de la empresa.')
    });
  }

  nueva(): void { this.editingId.set(null); this.form.reset({ nombre: '' }); this.formVisible.set(true); }
  editar(empresa: EmpresaRaiz): void { this.editingId.set(empresa.id); this.form.reset({ nombre: empresa.nombre }); this.formVisible.set(true); }
  cancelarEdicion(): void { this.formVisible.set(false); this.editingId.set(null); this.form.reset({ nombre: '' }); }

  guardar(): void {
    const nombre = this.form.controls.nombre.value?.trim() ?? '';
    if (!nombre || this.form.invalid) { this.form.controls.nombre.markAsTouched(); return; }
    const id = this.editingId();
    this.saving.set(true);
    const request = id
      ? this.http.put<ApiResponse<EmpresaRaiz>>(`${this.apiUrl}/${id}`, { nombre })
      : this.http.post<ApiResponse<EmpresaRaiz>>(this.apiUrl, { nombre });
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => { this.cancelarEdicion(); this.snackBar.open(id ? 'Empresa actualizada.' : 'Empresa creada.', 'Cerrar', { duration: 3500 }); this.cargar(); },
      error: err => this.error.set(err.error?.message ?? 'No se pudo guardar la empresa.')
    });
  }

  cambiarActivo(empresa: EmpresaRaiz, activa: boolean): void {
    this.operandoId.set(empresa.id);
    const accion = activa ? 'activar' : 'desactivar';
    this.http.patch<ApiResponse<EmpresaRaiz>>(`${this.apiUrl}/${empresa.id}/${accion}`, {}).pipe(finalize(() => this.operandoId.set(null))).subscribe({
      next: res => { this.empresas.update(items => items.map(item => item.id === empresa.id ? res.data : item)); this.detalle.update(actual => actual?.id === empresa.id ? res.data : actual); },
      error: err => this.error.set(err.error?.message ?? `No se pudo ${accion} la empresa.`)
    });
  }

  private noWhitespaceValidator(control: { value: unknown }): null | { whitespace: true } {
    return typeof control.value === 'string' && control.value.length > 0 && control.value.trim().length === 0 ? { whitespace: true } : null;
  }
}
