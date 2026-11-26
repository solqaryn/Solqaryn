import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';

import { Component, OnInit, OnDestroy, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { PAGINADOR_ESPANOL_PROVIDER } from '../../shared/paginador-institucional';
import { finalize, Subscription } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CentroCosto, TipoCentroCosto } from '../../core/models/centro-costo.model';
import { CentroCostoService } from '../../services/centro-costo.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  viewProviders: [PAGINADOR_ESPANOL_PROVIDER],
  selector: 'app-centros-costo',
  standalone: true,
  imports: [ConsultaRegistroDirective, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, MatSlideToggleModule, MatPaginatorModule],
  template: `
    <section class="page-shell">
      <header class="page-header">
        <div><h1 class="page-title">Centros de costo</h1><p class="page-subtitle">Clasifica las operaciones contables por sucursal, departamento, proyecto o unidad de negocio para facilitar su seguimiento.</p></div>
        @if (puedeCrear()) { <button mat-flat-button color="primary" type="button" (click)="nuevo()"><mat-icon>add</mat-icon> Nuevo centro</button> }
      </header>

      @if (mostrandoFormulario()) {
        <form class="card editor" [formGroup]="form" (ngSubmit)="guardar()" aria-label="Formulario de centro de costo">
          <div class="editor-title"><h2>{{ editandoId() ? 'Editar centro de costo' : 'Nuevo centro de costo' }}</h2><button mat-icon-button type="button" (click)="cancelar()" aria-label="Cerrar formulario"><mat-icon>close</mat-icon></button></div>
          <div class="form-grid">
            <mat-form-field appearance="outline"><mat-label>Código</mat-label><input matInput formControlName="codigo" maxlength="50" autocomplete="off">@if (form.controls.codigo.hasError('required')) { <mat-error>El código es obligatorio.</mat-error> }</mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Nombre</mat-label><input matInput formControlName="nombre" maxlength="150" autocomplete="off">@if (form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio.</mat-error> }</mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Tipo</mat-label><mat-select formControlName="tipo">@for (tipo of tipos; track tipo.value) { <mat-option [value]="tipo.value">{{ tipo.label }}</mat-option> }</mat-select></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Identificador de sucursal</mat-label><input matInput type="number" min="1" formControlName="sucursalId"><mat-hint>Indica el identificador de la sucursal solo cuando el tipo sea Sucursal.</mat-hint></mat-form-field>
          </div>
          <mat-form-field appearance="outline" class="full"><mat-label>Descripción</mat-label><textarea matInput rows="3" formControlName="descripcion" maxlength="500"></textarea></mat-form-field>
          @if (editandoId()) { <mat-slide-toggle formControlName="activo">Activo</mat-slide-toggle> }
          @if (errorFormulario()) { <p class="error" role="alert">{{ errorFormulario() }}</p> }
          <div class="actions"><button mat-button type="button" (click)="cancelar()">Cancelar</button><button mat-flat-button color="primary" type="submit" [disabled]="saving()">@if (saving()) { <mat-spinner diameter="20"></mat-spinner> } @if (!saving()) { <mat-icon>save</mat-icon> }<span>{{ saving() ? 'Guardando…' : 'Guardar' }}</span></button></div>
        </form>
      }
      <section class="card app-list-surface" aria-label="Listado de centros de costo">
        <div class="app-table-filters" role="search" aria-label="Filtros de centros de costo">
          <mat-form-field appearance="outline" class="search-field"><mat-label>Código o nombre</mat-label><input matInput [value]="termino()" (input)="actualizarTermino($event)" (keyup.enter)="buscar()" aria-label="Buscar centros de costo"></mat-form-field>
          <div class="filter-actions"><button mat-flat-button color="primary" type="button" (click)="buscar()"><mat-icon>search</mat-icon> Buscar</button><button mat-button type="button" (click)="limpiar()">Limpiar</button></div>
        </div>
        @if (loading()) { <div class="state" role="status"><mat-spinner diameter="38"></mat-spinner><span>Cargando centros de costo…</span></div> }
        @else if (errorCarga()) { <div class="state error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ errorCarga() }}</span><button mat-button type="button" (click)="cargar()">Reintentar</button></div> }
        @else if (items().length === 0) { <div class="app-list-empty" role="status"><mat-icon aria-hidden="true">account_tree</mat-icon><h2>Sin centros de costo</h2><p>{{ filtroAplicado ? 'No se encontraron centros de costo con ese código o nombre. Cambia la búsqueda.' : 'Los centros de costo registrados aparecerán aquí para su consulta y administración.' }}</p></div> }
        @else {
          <div class="app-table-shell" role="region" aria-label="Tabla de centros de costo" tabindex="0"><table class="app-data-table"><thead><tr><th>Código</th><th>Nombre</th><th>Tipo</th><th>Sucursal</th><th>Estado</th><th class="actions">Acciones</th></tr></thead><tbody>
          @for (item of items(); track item.id) { <tr [class.inactive]="!item.activo"><td><code>{{ item.codigo }}</code></td><td><strong>{{ item.nombre }}</strong></td><td>{{ tipoNombre(item.tipo) }}</td><td>{{ item.sucursalNombre || item.sucursalCodigo || '—' }}</td><td>@if (puedeCambiarEstado(item)) { <mat-slide-toggle [checked]="item.activo" [disabled]="accionEnCurso() !== null" (change)="cambiarEstado(item, $event)" [aria-label]="(item.activo ? 'Desactivar ' : 'Activar ') + item.nombre"></mat-slide-toggle> } @else { <span class="status" [class.ok]="item.activo">{{ item.activo ? 'Activo' : 'Inactivo' }}</span> }</td><td class="actions">
            <button class="action-view" mat-icon-button type="button" title="Ver detalle" [attr.aria-label]="'Ver ' + item.nombre" [appConsultaRegistro]="{ titulo: 'Centro de costo: ' + item.nombre, campos: { 'Código': item.codigo, 'Nombre': item.nombre, 'Descripción': item.descripcion, 'Tipo': tipoNombre(item.tipo), 'Sucursal': item.sucursalNombre || item.sucursalCodigo, 'Estado': item.activo ? 'Activo' : 'Inactivo', 'Fecha de creación': item.fechaCreacion, 'Fecha de actualización': item.fechaActualizacion } }"><mat-icon>visibility</mat-icon></button>
            @if (puedeEditar()) { <button class="action-edit" mat-icon-button type="button" (click)="editar(item)" [attr.aria-label]="'Editar ' + item.nombre"><mat-icon>edit</mat-icon></button> }
            @if (puedeEliminar()) { <button class="action-delete" mat-icon-button type="button" [disabled]="accionEnCurso() !== null" (click)="eliminar(item)" [attr.aria-label]="'Eliminar ' + item.nombre"><mat-icon>delete_outline</mat-icon></button> }
          </td></tr> }
          </tbody></table></div>
        }
        @if (!loading() && !errorCarga() && total() > 0) {
          <mat-paginator [length]="total()" [pageIndex]="page - 1" [pageSize]="pageSize" [pageSizeOptions]="[10,20,50,100]" (page)="cambiarPagina($event)" aria-label="Paginación de centros de costo"></mat-paginator>
        }
      </section>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display:block; min-width:0; }
    .page-shell { display:grid; grid-template-columns:minmax(0,1fr); gap:20px; min-width:0; }
    .page-header { display:flex; justify-content:space-between; gap:16px; align-items:flex-start; }
    .page-header > div { min-width:0; }
    .page-header h1 { margin:0 0 6px; }
    .page-header p { margin:0; max-width:70ch; color:var(--color-text-muted); }
    .editor { min-width:0; }
    .editor-title { display:flex; justify-content:space-between; align-items:center; gap:12px; }
    .editor-title h2 { margin:0 0 12px; font-size:1.2rem; }
    .form-grid { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:14px; }
    .full { width:100%; }
    td.actions { white-space:nowrap; }
    .editor > .actions { display:flex; flex-wrap:wrap; justify-content:flex-end; gap:10px; margin-top:14px; }
    .state { min-height:160px; display:flex; flex-wrap:wrap; align-items:center; justify-content:center; gap:10px; color:var(--color-text-muted); text-align:center; overflow-wrap:anywhere; }
    .error { color:var(--color-danger); }
    table { min-width:760px; }
    td:nth-child(2) { min-width:140px; overflow-wrap:anywhere; }
    .status { display:inline-flex; padding:3px 8px; border-radius:999px; background:color-mix(in srgb,var(--color-danger) 12%,var(--color-surface)); color:var(--color-text); }
    .status.ok { background:color-mix(in srgb,var(--color-success) 12%,var(--color-surface)); }
    @media(max-width:760px) { .page-header { flex-direction:column; } .form-grid { grid-template-columns:minmax(0,1fr); } }
    @media(max-width:480px) { .editor > .actions { flex-direction:column-reverse; } .editor > .actions button { width:100%; } }
  `]
})
export class CentrosCostoComponent implements OnInit, OnDestroy {
  readonly total = signal(0);
  readonly accionEnCurso = signal<number | null>(null);
  page = 1;
  pageSize = 20;
  filtroAplicado = '';
  private carga?: Subscription;
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
  ngOnDestroy(): void { this.carga?.unsubscribe(); }
  buscar(): void { this.filtroAplicado = this.termino().trim(); this.page = 1; this.cargar(); }
  limpiar(): void { this.termino.set(''); this.buscar(); }
  cambiarPagina(event: PageEvent): void { this.page = event.pageIndex + 1; this.pageSize = event.pageSize; this.cargar(); }
  cargar(): void {
    this.carga?.unsubscribe();
    this.loading.set(true);
    this.errorCarga.set('');
    this.carga = this.service.buscar({ termino: this.filtroAplicado, pagina: this.page, tamanoPagina: this.pageSize }).subscribe({
      next: response => {
        this.loading.set(false);
        if (!response.success || !response.data) {
          this.items.set([]); this.total.set(0);
          this.errorCarga.set(response.message || 'No se pudieron cargar los centros de costo.');
          return;
        }
        this.items.set(response.data.items ?? []);
        this.total.set(response.data.total);
      },
      error: () => {
        this.loading.set(false); this.items.set([]); this.total.set(0);
        this.errorCarga.set('No se pudieron cargar los centros de costo.');
      }
    });
  }
  nuevo(): void { if (!this.puedeCrear()) return; this.editandoId.set(null); this.errorFormulario.set(''); this.form.reset({ codigo: '', nombre: '', descripcion: '', tipo: TipoCentroCosto.Sucursal, sucursalId: null, activo: true }); this.mostrandoFormulario.set(true); }
  editar(item: CentroCosto): void { if (!this.puedeEditar()) return; this.editandoId.set(item.id); this.errorFormulario.set(''); this.form.reset({ codigo: item.codigo, nombre: item.nombre, descripcion: item.descripcion ?? '', tipo: item.tipo, sucursalId: item.sucursalId ?? null, activo: item.activo }); this.mostrandoFormulario.set(true); }
  cancelar(): void { this.mostrandoFormulario.set(false); this.editandoId.set(null); this.errorFormulario.set(''); }
  guardar(): void { const permitido = this.editandoId() ? this.puedeEditar() : this.puedeCrear(); if (!permitido || !this.mostrandoFormulario()) return; if (this.form.invalid || this.saving()) { this.form.markAllAsTouched(); return; } const raw = this.form.getRawValue(); const sucursalId = raw.sucursalId == null ? null : Number(raw.sucursalId); if (raw.tipo === TipoCentroCosto.Sucursal && (sucursalId === null || !Number.isInteger(sucursalId) || sucursalId <= 0)) { this.errorFormulario.set('Indica la sucursal para crear o editar un centro de tipo Sucursal.'); return; } if (raw.tipo !== TipoCentroCosto.Sucursal && sucursalId !== null) { this.errorFormulario.set('Deja la sucursal vacía cuando el centro tenga otro tipo.'); return; } const base = { codigo: raw.codigo!.trim(), nombre: raw.nombre!.trim(), descripcion: raw.descripcion?.trim() || null, tipo: raw.tipo!, sucursalId }; this.saving.set(true); const request = this.editandoId() ? this.service.update(this.editandoId()!, { ...base, activo: Boolean(raw.activo) }) : this.service.create(base); this.errorFormulario.set(''); request.subscribe({ next: response => { this.saving.set(false); if (!response.success) { this.errorFormulario.set(response.message || 'No se pudo guardar el centro de costo.'); return; } this.cancelar(); this.snack.open('Centro de costo guardado.', 'Cerrar', { duration: 3000 }); this.cargar(); }, error: () => { this.saving.set(false); this.errorFormulario.set('No se pudo guardar el centro de costo.'); } }); }
  puedeCambiarEstado(item: CentroCosto): boolean { return item.activo ? this.puedeDesactivar() : this.puedeActivar(); }
  cambiarEstado(item: CentroCosto, event?: MatSlideToggleChange): void {
    // El estado visual representa el último dato confirmado por la API.
    if (event) event.source.checked = item.activo;
    if (!this.puedeCambiarEstado(item) || this.accionEnCurso() !== null) return;
    this.accionEnCurso.set(item.id);
    this.service.cambiarEstado(item.id, !item.activo)
      .pipe(finalize(() => this.accionEnCurso.set(null)))
      .subscribe({
        next: response => {
          if (!response.success || !response.data) {
            this.snack.open(response.message || 'No se pudo cambiar el estado.', 'Cerrar', { duration: 3000 });
            return;
          }
          this.cargar();
        },
        error: () => this.snack.open('No se pudo cambiar el estado.', 'Cerrar', { duration: 3000 })
      });
  }
  async eliminar(item: CentroCosto): Promise<void> {
    if (!this.puedeEliminar() || this.accionEnCurso() !== null) return;
    this.accionEnCurso.set(item.id);
    let confirmado: boolean;
    try {
      confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar centro de costo',
      mensaje: `El centro ${item.codigo} dejará de estar disponible para nuevas operaciones.`,
      detalle: 'El historial relacionado se conservará.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
      });
    } catch {
      this.accionEnCurso.set(null);
      this.snack.open('No se pudo abrir la confirmación.', 'Cerrar', { duration: 3000 });
      return;
    }
    if (!confirmado || !this.puedeEliminar()) {
      this.accionEnCurso.set(null);
      return;
    }
    this.service.delete(item.id)
      .pipe(finalize(() => this.accionEnCurso.set(null)))
      .subscribe({
        next: response => {
          if (!response.success) {
            this.snack.open(response.message || 'No se pudo eliminar el centro de costo.', 'Cerrar', { duration: 3000 });
            return;
          }
          this.page = 1;
          this.cargar();
        },
        error: () => this.snack.open('No se pudo eliminar el centro de costo.', 'Cerrar', { duration: 3000 })
      });
  }
  tipoNombre(tipo: TipoCentroCosto): string { return this.tipos.find(x => x.value === tipo)?.label ?? String(tipo); }
}
