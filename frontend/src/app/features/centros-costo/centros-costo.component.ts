
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CentroCosto, TipoCentroCosto } from '../../core/models/centro-costo.model';
import { CentroCostoService } from '../../services/centro-costo.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-centros-costo',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, MatSlideToggleModule],
  template: `
    <section class="page-shell">
      <header class="page-header">
        <div><p class="eyebrow">Finanzas</p><h1>Centros de costo</h1><p>Administra la clasificación contable por sucursal, departamento, proyecto o unidad de negocio.</p></div>
        @if (puedeCrear()) { <button mat-flat-button color="primary" type="button" (click)="nuevo()"><mat-icon>add</mat-icon> Nuevo centro</button> }
      </header>
      <div class="toolbar" role="search">
        <mat-form-field appearance="outline"><mat-label>Buscar</mat-label><input matInput [value]="termino()" (input)="actualizarTermino($event)" (keyup.enter)="cargar()" aria-label="Buscar centros de costo"></mat-form-field>
        <button mat-stroked-button type="button" (click)="cargar()">Aplicar</button>
      </div>
      @if (mostrandoFormulario()) {
        <form class="editor" [formGroup]="form" (ngSubmit)="guardar()" aria-label="Formulario de centro de costo">
          <div class="editor-title"><h2>{{ editandoId() ? 'Editar centro de costo' : 'Nuevo centro de costo' }}</h2><button mat-icon-button type="button" (click)="cancelar()" aria-label="Cerrar formulario"><mat-icon>close</mat-icon></button></div>
          <div class="form-grid">
            <mat-form-field appearance="outline"><mat-label>Código</mat-label><input matInput formControlName="codigo" maxlength="50" autocomplete="off">@if (form.controls.codigo.hasError('required')) { <mat-error>El código es obligatorio.</mat-error> }</mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Nombre</mat-label><input matInput formControlName="nombre" maxlength="150" autocomplete="off">@if (form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio.</mat-error> }</mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Tipo</mat-label><mat-select formControlName="tipo">@for (tipo of tipos; track tipo.value) { <mat-option [value]="tipo.value">{{ tipo.label }}</mat-option> }</mat-select></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Sucursal ID</mat-label><input matInput type="number" min="1" formControlName="sucursalId"><mat-hint>Obligatoria únicamente cuando el tipo es Sucursal.</mat-hint></mat-form-field>
          </div>
          <mat-form-field appearance="outline" class="full"><mat-label>Descripción</mat-label><textarea matInput rows="3" formControlName="descripcion" maxlength="500"></textarea></mat-form-field>
          @if (editandoId()) { <mat-slide-toggle formControlName="activo">Activo</mat-slide-toggle> }
          @if (errorFormulario()) { <p class="error" role="alert">{{ errorFormulario() }}</p> }
          <div class="actions"><button mat-button type="button" (click)="cancelar()">Cancelar</button><button mat-flat-button color="primary" type="submit" [disabled]="saving()">@if (saving()) { <mat-spinner diameter="20"></mat-spinner> } @else { <mat-icon>save</mat-icon> Guardar }</button></div>
        </form>
      }
      <div class="card" aria-live="polite">
        @if (loading()) { <div class="state"><mat-spinner diameter="38"></mat-spinner><span>Cargando centros de costo…</span></div> }
        @else if (errorCarga()) { <div class="state error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ errorCarga() }}</span><button mat-button type="button" (click)="cargar()">Reintentar</button></div> }
        @else if (items().length === 0) { <div class="state"><mat-icon>account_tree</mat-icon><h2>Sin centros de costo</h2><p>No hay registros para los filtros actuales.</p></div> }
        @else {
          <div class="table-scroll"><table><thead><tr><th>Código</th><th>Nombre</th><th>Tipo</th><th>Sucursal</th><th>Estado</th><th class="right">Acciones</th></tr></thead><tbody>
          @for (item of items(); track item.id) { <tr [class.inactive]="!item.activo"><td><code>{{ item.codigo }}</code></td><td><strong>{{ item.nombre }}</strong></td><td>{{ tipoNombre(item.tipo) }}</td><td>{{ item.sucursalNombre || item.sucursalCodigo || '—' }}</td><td><span class="status" [class.ok]="item.activo">{{ item.activo ? 'Activo' : 'Inactivo' }}</span></td><td class="right">
            @if (puedeEditar()) { <button mat-icon-button type="button" (click)="editar(item)" [attr.aria-label]="'Editar ' + item.nombre"><mat-icon>edit</mat-icon></button> }
            @if (puedeCambiarEstado(item)) { <button mat-icon-button type="button" (click)="cambiarEstado(item)" [attr.aria-label]="(item.activo ? 'Desactivar ' : 'Activar ') + item.nombre"><mat-icon>{{ item.activo ? 'toggle_off' : 'toggle_on' }}</mat-icon></button> }
            @if (puedeEliminar()) { <button mat-icon-button type="button" (click)="eliminar(item)" [attr.aria-label]="'Eliminar ' + item.nombre"><mat-icon>delete_outline</mat-icon></button> }
          </td></tr> }
          </tbody></table></div>
        }
      </div>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`.page-shell{display:grid;gap:20px}.page-header{display:flex;justify-content:space-between;gap:20px;align-items:flex-start}.page-header h1{margin:2px 0 6px;font-size:clamp(1.6rem,2.5vw,2.2rem)}.page-header p{margin:0;color:var(--text-secondary,#64748b)}.eyebrow{text-transform:uppercase;letter-spacing:.09em;font-weight:700;font-size:.75rem;color:var(--primary,#2563eb)!important}.toolbar{display:flex;gap:10px;align-items:center}.toolbar mat-form-field{min-width:min(420px,70vw)}.editor,.card{background:var(--surface,#fff);border:1px solid var(--border,#e2e8f0);border-radius:16px;padding:20px}.editor-title{display:flex;justify-content:space-between;align-items:center}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}.full{width:100%}.actions{display:flex;justify-content:flex-end;gap:10px;margin-top:14px}.state{min-height:220px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;color:var(--text-secondary,#64748b);text-align:center}.error{color:#b91c1c}.table-scroll{overflow:auto}table{width:100%;border-collapse:collapse;min-width:760px}th,td{text-align:left;padding:13px 10px;border-bottom:1px solid var(--border,#e2e8f0)}th{font-size:.75rem;text-transform:uppercase;letter-spacing:.05em;color:var(--text-secondary,#64748b)}.right{text-align:right;white-space:nowrap}.inactive{opacity:.65}.status{padding:3px 8px;border-radius:999px;background:#fee2e2;color:#991b1b}.status.ok{background:#dcfce7;color:#166534}@media(max-width:760px){.page-header{flex-direction:column}.toolbar{align-items:stretch;flex-direction:column}.toolbar mat-form-field{min-width:100%}.form-grid{grid-template-columns:1fr}}`]
})
export class CentrosCostoComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(CentroCostoService);
  private readonly snack = inject(MatSnackBar);
  private readonly permisosRuntime = inject(PermisosRuntimeService);
  private readonly alerts = inject(AppAlertService);
  readonly items = signal<CentroCosto[]>([]); readonly loading = signal(true); readonly saving = signal(false); readonly termino = signal(''); readonly mostrandoFormulario = signal(false); readonly editandoId = signal<number | null>(null); readonly errorCarga = signal(''); readonly errorFormulario = signal('');
  readonly puedeCrear = signal(false); readonly puedeEditar = signal(false); readonly puedeActivar = signal(false); readonly puedeDesactivar = signal(false); readonly puedeEliminar = signal(false);
  readonly tipos = [{ value: TipoCentroCosto.Sucursal, label: 'Sucursal' }, { value: TipoCentroCosto.Departamento, label: 'Departamento' }, { value: TipoCentroCosto.Proyecto, label: 'Proyecto' }, { value: TipoCentroCosto.UnidadNegocio, label: 'Unidad de negocio' }];
  readonly form = this.fb.group({ codigo: ['', [Validators.required, Validators.maxLength(50)]], nombre: ['', [Validators.required, Validators.maxLength(150)]], descripcion: [''], tipo: [TipoCentroCosto.Sucursal, Validators.required], sucursalId: [null as number | null], activo: [true] });
  ngOnInit(): void { this.puedeCrear.set(this.permisosRuntime.puede('Finanzas', 'Crear')); this.puedeEditar.set(this.permisosRuntime.puede('Finanzas', 'Editar')); this.puedeActivar.set(this.permisosRuntime.puede('Finanzas', 'Activar')); this.puedeDesactivar.set(this.permisosRuntime.puede('Finanzas', 'Desactivar')); this.puedeEliminar.set(this.permisosRuntime.puede('Finanzas', 'EliminarLogico')); this.cargar(); }
  actualizarTermino(event: Event): void { this.termino.set((event.target as HTMLInputElement).value); }
  cargar(): void { this.loading.set(true); this.errorCarga.set(''); this.service.buscar({ termino: this.termino(), pagina: 1, tamanoPagina: 100 }).subscribe({ next: ({ data }) => { this.items.set(data.items ?? []); this.loading.set(false); }, error: () => { this.loading.set(false); this.errorCarga.set('No se pudieron cargar los centros de costo.'); } }); }
  nuevo(): void { if (!this.puedeCrear()) return; this.editandoId.set(null); this.errorFormulario.set(''); this.form.reset({ codigo: '', nombre: '', descripcion: '', tipo: TipoCentroCosto.Sucursal, sucursalId: null, activo: true }); this.mostrandoFormulario.set(true); }
  editar(item: CentroCosto): void { if (!this.puedeEditar()) return; this.editandoId.set(item.id); this.errorFormulario.set(''); this.form.reset({ codigo: item.codigo, nombre: item.nombre, descripcion: item.descripcion ?? '', tipo: item.tipo, sucursalId: item.sucursalId ?? null, activo: item.activo }); this.mostrandoFormulario.set(true); }
  cancelar(): void { this.mostrandoFormulario.set(false); this.editandoId.set(null); this.errorFormulario.set(''); }
  guardar(): void { const permitido = this.editandoId() ? this.puedeEditar() : this.puedeCrear(); if (!permitido) return; if (this.form.invalid || this.saving()) { this.form.markAllAsTouched(); return; } const raw = this.form.getRawValue(); const sucursalId = raw.sucursalId ? Number(raw.sucursalId) : null; if (raw.tipo === TipoCentroCosto.Sucursal && !sucursalId) { this.errorFormulario.set('Sucursal ID es obligatoria para centros de tipo Sucursal.'); return; } if (raw.tipo !== TipoCentroCosto.Sucursal && sucursalId) { this.errorFormulario.set('Sucursal ID debe quedar vacía para tipos distintos de Sucursal.'); return; } const base = { codigo: raw.codigo!.trim(), nombre: raw.nombre!.trim(), descripcion: raw.descripcion?.trim() || null, tipo: raw.tipo!, sucursalId }; this.saving.set(true); const request = this.editandoId() ? this.service.update(this.editandoId()!, { ...base, activo: Boolean(raw.activo) }) : this.service.create(base); request.subscribe({ next: () => { this.saving.set(false); this.cancelar(); this.snack.open('Centro de costo guardado.', 'Cerrar', { duration: 3000 }); this.cargar(); }, error: () => { this.saving.set(false); this.errorFormulario.set('No se pudo guardar el centro de costo.'); } }); }
  puedeCambiarEstado(item: CentroCosto): boolean { return item.activo ? this.puedeDesactivar() : this.puedeActivar(); }
  cambiarEstado(item: CentroCosto): void { if (!this.puedeCambiarEstado(item)) return; this.service.cambiarEstado(item.id, !item.activo).subscribe({ next: () => this.cargar(), error: () => this.snack.open('No se pudo cambiar el estado.', 'Cerrar', { duration: 3000 }) }); }
  async eliminar(item: CentroCosto): Promise<void> {
    if (!this.puedeEliminar()) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar centro de costo',
      mensaje: `Se eliminará lógicamente el centro ${item.codigo}.`,
      detalle: 'El historial relacionado se conservará.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;
    this.service.delete(item.id).subscribe({ next: () => this.cargar(), error: () => this.snack.open('No se pudo eliminar el centro de costo.', 'Cerrar', { duration: 3000 }) });
  }
  tipoNombre(tipo: TipoCentroCosto): string { return this.tipos.find(x => x.value === tipo)?.label ?? String(tipo); }
}
