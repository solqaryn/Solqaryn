import { TenantContextoVerificado } from '../../core/auth/tenant-context.service';
import { crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';
import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';

import { Component, OnInit, inject, signal, computed, ChangeDetectionStrategy, TemplateRef, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule, MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { modalInstitucional } from '../../shared/modal-institucional/modal-institucional';
import { firstValueFrom } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MetodoPago, MetodoPagoCreate, MetodoPagoUpdate } from '../../core/models/metodo-pago.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { MetodoPagoService } from '../../services/metodo-pago.service';

@Component({
  selector: 'app-metodos-pago',
  standalone: true,
  imports: [ConsultaRegistroDirective,
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule, MatDialogModule
],
  template: `
    <section class="page-shell">
      <header class="page-header">
        <div>
          <p class="eyebrow">Configuración financiera</p>
          <h1>Métodos de pago</h1>
          <p>Administra el catálogo relacional utilizado por ventas, pagos y operaciones financieras.</p>
        </div>
        @if (puedeCrear()) {
          <button mat-flat-button color="primary" type="button" (click)="nuevo()">
            <mat-icon>add</mat-icon> Nuevo método
          </button>
        }
      </header>

      <ng-template #formularioModal>
        <form class="app-dialog-layout" [formGroup]="form" (ngSubmit)="guardar()">
          <h2 mat-dialog-title>{{ editandoId() ? 'Editar método de pago' : 'Nuevo método de pago' }}</h2>
          <mat-dialog-content class="app-dialog-layout__body">

          <div class="form-grid">
            <mat-form-field appearance="outline">
              <mat-label>Código</mat-label>
              <input matInput formControlName="codigo" maxlength="50" autocomplete="off">
              @if (form.controls.codigo.hasError('required')) { <mat-error>El código es obligatorio.</mat-error> }
              @if (form.controls.codigo.hasError('pattern')) { <mat-error>Usa letras, números, guion o guion bajo.</mat-error> }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Nombre</mat-label>
              <input matInput formControlName="nombre" maxlength="120" autocomplete="off">
              @if (form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio.</mat-error> }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Tipo</mat-label>
              <mat-select formControlName="tipo">
                @for (tipo of tipos; track tipo) { <mat-option [value]="tipo">{{ tipo }}</mat-option> }
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Orden</mat-label>
              <input matInput type="number" min="0" formControlName="orden">
              @if (form.controls.orden.hasError('min')) { <mat-error>El orden no puede ser negativo.</mat-error> }
            </mat-form-field>
          </div>

          <div class="flags">
            <mat-slide-toggle formControlName="activo">Activo</mat-slide-toggle>
            <mat-checkbox formControlName="requiereReferencia">Requiere referencia</mat-checkbox>
            <mat-checkbox formControlName="requiereBanco">Requiere banco</mat-checkbox>
            <mat-checkbox formControlName="permiteCambio">Permite cambio</mat-checkbox>
          </div>

          <mat-form-field appearance="outline" class="metadata-field">
            <mat-label>Metadata JSON</mat-label>
            <textarea matInput rows="4" formControlName="metadata" placeholder='{"clave":"valor"}'></textarea>
            <mat-hint>Opcional. Debe ser un objeto JSON válido.</mat-hint>
          </mat-form-field>

          @if (metadataError()) { <p class="form-error" role="alert">{{ metadataError() }}</p> }

          </mat-dialog-content>
          <mat-dialog-actions align="end">
            <button mat-button type="button" [disabled]="saving()" (click)="cancelar()">Cancelar</button>
            <button mat-flat-button color="primary" type="submit" [disabled]="saving()">
              @if (saving()) { <mat-spinner diameter="20"></mat-spinner> }
              @if (!saving()) { <mat-icon>save</mat-icon> }
              <span>{{ saving() ? 'Guardando…' : 'Guardar' }}</span>
            </button>
          </mat-dialog-actions>
        </form>
      </ng-template>

      <div class="list-card">
        <div class="app-table-filters" aria-label="Filtros de métodos de pago">
          <mat-form-field appearance="outline" subscriptSizing="dynamic"><mat-label>Buscar por nombre o código</mat-label><mat-icon matPrefix>search</mat-icon><input matInput #busqueda [value]="buscar()" (input)="buscar.set(busqueda.value)"></mat-form-field>
          <mat-form-field appearance="outline" subscriptSizing="dynamic"><mat-label>Estado</mat-label><mat-select [value]="estadoFiltro()" (selectionChange)="estadoFiltro.set($event.value)"><mat-option value="todos">Todos</mat-option><mat-option value="activos">Activos</mat-option><mat-option value="inactivos">Inactivos</mat-option></mat-select></mat-form-field>
          <mat-form-field appearance="outline" subscriptSizing="dynamic"><mat-label>Tipo</mat-label><mat-select [value]="tipoFiltro()" (selectionChange)="tipoFiltro.set($event.value)"><mat-option value="todos">Todos</mat-option>@for (tipo of tipos; track tipo) { <mat-option [value]="tipo">{{ tipo }}</mat-option> }</mat-select></mat-form-field>
          <button mat-stroked-button type="button" (click)="limpiarFiltros()"><mat-icon>filter_alt_off</mat-icon> Limpiar filtros</button>
        </div>
        @if (loading()) {
          <div class="loading"><mat-spinner diameter="38"></mat-spinner><span>Cargando métodos de pago…</span></div>
        } @else if (visibles().length === 0) {
          <div class="empty"><mat-icon>payments</mat-icon><h2>Sin métodos de pago</h2><p>No hay registros disponibles.</p></div>
        } @else {
          <div class="app-table-shell">
            <table class="app-data-table">
              <thead><tr><th>Orden</th><th>Código</th><th>Nombre</th><th>Tipo</th><th>Reglas</th><th class="app-col-center">Estado</th><th class="actions-col">Acciones</th></tr></thead>
              <tbody>
                @for (metodo of visibles(); track metodo.id; let i = $index) {
                  <tr [class.inactivo]="!metodo.activo">
                    <td class="order-cell">
                      <span>{{ metodo.orden }}</span>
                      @if (puedeEditar()) {
                        <span class="order-buttons">
                          <button mat-icon-button type="button" [disabled]="filtrado() || i === 0 || reordering()" (click)="mover(i, -1)" aria-label="Subir método"><mat-icon>keyboard_arrow_up</mat-icon></button>
                          <button mat-icon-button type="button" [disabled]="filtrado() || i === metodos().length - 1 || reordering()" (click)="mover(i, 1)" aria-label="Bajar método"><mat-icon>keyboard_arrow_down</mat-icon></button>
                        </span>
                      }
                    </td>
                    <td><code>{{ metodo.codigo }}</code></td>
                    <td><strong>{{ metodo.nombre }}</strong></td>
                    <td>{{ metodo.tipo }}</td>
                    <td>
                      <div class="chips">
                        @if (metodo.requiereReferencia) { <span>Referencia</span> }
                        @if (metodo.requiereBanco) { <span>Banco</span> }
                        @if (metodo.permiteCambio) { <span>Cambio</span> }
                        @if (!metodo.requiereReferencia && !metodo.requiereBanco && !metodo.permiteCambio) { <span class="muted-chip">Sin reglas extra</span> }
                      </div>
                    </td>
                    <td class="app-col-center">@if (puedeCambiarEstado(metodo)) {
                      <mat-slide-toggle [checked]="metodo.activo" [disabled]="cambiandoEstado()" (change)="cambiarEstado(metodo, $event)" [aria-label]="(metodo.activo ? 'Desactivar ' : 'Activar ') + metodo.nombre"></mat-slide-toggle>
                    } @else { <span class="status" [class.ok]="metodo.activo">{{ metodo.activo ? 'Activo' : 'Inactivo' }}</span> }</td>
                    <td class="actions">
                      <button type="button" class="action-view" mat-icon-button title="Ver detalle" [attr.aria-label]="'Ver ' + metodo.nombre" [appConsultaRegistro]="{ titulo: 'Método de pago: ' + metodo.nombre, campos: { 'Nombre': metodo.nombre, 'Código': metodo.codigo, 'Tipo': metodo.tipo, 'Estado': metodo.activo ? 'Activo' : 'Inactivo', 'Requiere referencia': metodo.requiereReferencia, 'Requiere banco': metodo.requiereBanco, 'Permite cambio': metodo.permiteCambio, 'Orden': metodo.orden } }"><mat-icon>visibility</mat-icon></button>
                      @if (puedeEditar()) { <button mat-icon-button type="button" (click)="editar(metodo)" aria-label="Editar método"><mat-icon>edit</mat-icon></button> }
                      @if (puedeEliminar()) { <button mat-icon-button type="button" (click)="eliminar(metodo)" aria-label="Eliminar método"><mat-icon>delete_outline</mat-icon></button> }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <footer class="app-table-footer" aria-live="polite">{{ visibles().length }} de {{ metodos().length }} registro(s)</footer>
        }
      </div>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page-shell{display:grid;gap:20px}.page-header{display:flex;justify-content:space-between;gap:20px;align-items:flex-start}.page-header h1{margin:2px 0 6px;font-size:clamp(1.6rem,2.5vw,2.2rem)}.page-header p{margin:0;color:var(--text-secondary,#64748b)}.eyebrow{text-transform:uppercase;letter-spacing:.09em;font-weight:700;font-size:.75rem;color:var(--primary,#2563eb)!important}.editor,.list-card{background:var(--surface,#fff);border:1px solid var(--border,#e2e8f0);border-radius:16px;padding:20px;box-shadow:0 8px 24px rgba(15,23,42,.05)}.editor-title{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;margin-bottom:18px}.editor-title h2{margin:0 0 4px}.editor-title p{margin:0;color:var(--text-secondary,#64748b)}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}.flags{display:flex;flex-wrap:wrap;gap:18px 28px;align-items:center;margin:4px 0 18px}.metadata-field{width:100%}.form-actions{display:flex;justify-content:flex-end;gap:10px;align-items:center}.form-error{color:#b91c1c;margin:0 0 12px}.loading,.empty{min-height:220px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:12px;color:var(--text-secondary,#64748b)}.empty mat-icon{font-size:42px;width:42px;height:42px}.empty h2,.empty p{margin:0}.table-scroll{overflow:auto}table{width:100%;border-collapse:collapse;min-width:900px}th,td{text-align:left;padding:13px 10px;border-bottom:1px solid var(--border,#e2e8f0);vertical-align:middle}th{font-size:.75rem;text-transform:uppercase;letter-spacing:.05em;color:var(--text-secondary,#64748b)}tr.inactivo{opacity:.68}.actions{white-space:nowrap}.order-cell{white-space:nowrap}.order-cell>span{vertical-align:middle}.order-buttons{display:inline-flex}.order-buttons button{width:30px;height:30px}.chips{display:flex;gap:5px;flex-wrap:wrap}.chips span,.status{display:inline-flex;padding:3px 8px;border-radius:999px;background:#eef2ff;font-size:.76rem}.chips .muted-chip{background:#f1f5f9}.status{background:#fee2e2;color:#991b1b}.status.ok{background:#dcfce7;color:#166534}code{font-size:.85rem}@media(max-width:760px){.page-header{flex-direction:column}.form-grid{grid-template-columns:1fr}.editor,.list-card{padding:14px}}
  `]
})
export class MetodosPagoComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(MetodoPagoService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog, { optional: true });
  @ViewChild('formularioModal') private formularioModal?: TemplateRef<unknown>;
  private editorRef?: MatDialogRef<unknown>;
  private contextoEditor: TenantContextoVerificado | null = null;
  readonly cambiandoEstado = signal(false);

  private abrirFormulario(): void {
    const contexto = this.acciones.contexto();
    if (!this.acciones.vigente(contexto, this.editandoId() ? 'Editar' : 'Crear')) return;
    this.contextoEditor = contexto;
    this.mostrandoFormulario.set(true);
    if (!this.formularioModal || !this.dialog) return;
    const ref = this.dialog.open(this.formularioModal, modalInstitucional('l', { ariaLabel: this.editandoId() ? 'Editar registro' : 'Crear registro' }));
    this.editorRef = ref;
    ref.afterClosed().subscribe(() => {
      if (this.editorRef !== ref) return;
      this.editorRef = undefined;
      this.mostrandoFormulario.set(false);
      this.editandoId.set(null);
    });
  }

  private readonly acciones = crearAccionesInstitucionales('MetodosPago');
  readonly metodos = signal<MetodoPago[]>([]);
  readonly buscar = signal('');
  readonly estadoFiltro = signal<'todos' | 'activos' | 'inactivos'>('todos');
  readonly tipoFiltro = signal('todos');
  readonly visibles = computed(() => {
    const termino = this.buscar().trim().toLocaleLowerCase('es-HN');
    return this.metodos().filter(item => (!termino || (item.nombre + ' ' + item.codigo).toLocaleLowerCase('es-HN').includes(termino)) &&
      (this.estadoFiltro() === 'todos' || item.activo === (this.estadoFiltro() === 'activos')) &&
      (this.tipoFiltro() === 'todos' || item.tipo === this.tipoFiltro()));
  });
  readonly filtrado = computed(() => !!this.buscar().trim() || this.estadoFiltro() !== 'todos' || this.tipoFiltro() !== 'todos');
  limpiarFiltros(): void { this.buscar.set(''); this.estadoFiltro.set('todos'); this.tipoFiltro.set('todos'); }
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly reordering = signal(false);
  readonly mostrandoFormulario = signal(false);
  readonly editandoId = signal<number | null>(null);
  readonly metadataError = signal('');

  readonly puedeCrear = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly tipos = ['Efectivo', 'Transferencia', 'Tarjeta', 'Cheque', 'Credito', 'Otro'];

  readonly form = this.fb.group({
    codigo: ['', [Validators.required, Validators.maxLength(50), Validators.pattern(/^[A-Za-z0-9_-]+$/)]],
    nombre: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(120)]],
    tipo: ['Otro', [Validators.required, Validators.maxLength(50)]],
    activo: [true],
    requiereReferencia: [false],
    requiereBanco: [false],
    permiteCambio: [false],
    orden: [0, [Validators.required, Validators.min(0)]],
    metadata: ['']
  });

  ngOnInit(): void {
    this.puedeCrear.set(this.permisos.puede('MetodosPago', 'Crear'));
    this.puedeEditar.set(this.permisos.puede('MetodosPago', 'Editar'));
    this.puedeActivar.set(this.permisos.puede('MetodosPago', 'Activar'));
    this.puedeDesactivar.set(this.permisos.puede('MetodosPago', 'Desactivar'));
    this.puedeEliminar.set(this.permisos.puede('MetodosPago', 'EliminarLogico'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: ({ data }) => {
        this.metodos.set([...data].sort((a, b) => a.orden - b.orden || a.codigo.localeCompare(b.codigo) || a.id - b.id));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error('No se pudieron cargar los métodos de pago.');
      }
    });
  }

  nuevo(): void {
    if (this.mostrandoFormulario()) return;
    this.editandoId.set(null);
    this.form.controls.codigo.enable();
    this.form.reset({ codigo: '', nombre: '', tipo: 'Otro', activo: true, requiereReferencia: false, requiereBanco: false, permiteCambio: false, orden: this.siguienteOrden(), metadata: '' });
    this.metadataError.set('');
    this.abrirFormulario();
  }

  editar(metodo: MetodoPago): void {
    if (this.mostrandoFormulario()) return;
    this.editandoId.set(metodo.id);
    this.form.reset({
      codigo: metodo.codigo,
      nombre: metodo.nombre,
      tipo: metodo.tipo || 'Otro',
      activo: metodo.activo,
      requiereReferencia: metodo.requiereReferencia,
      requiereBanco: metodo.requiereBanco,
      permiteCambio: metodo.permiteCambio,
      orden: metodo.orden,
      metadata: metodo.metadata ?? ''
    });
    this.form.controls.codigo.disable();
    this.metadataError.set('');
    this.abrirFormulario();
  }

  cancelar(): void {
    if (this.saving()) return;
    const ref = this.editorRef;
    this.editorRef = undefined;
    ref?.close();
    this.mostrandoFormulario.set(false);
    this.editandoId.set(null);
    this.form.controls.codigo.enable();
    this.metadataError.set('');
  }

  async guardar(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const metadata = this.normalizarMetadata(raw.metadata ?? '');
    if (metadata === undefined) return;

    const base = {
      nombre: raw.nombre!.trim(),
      tipo: raw.tipo!.trim(),
      activo: Boolean(raw.activo),
      requiereReferencia: Boolean(raw.requiereReferencia),
      requiereBanco: Boolean(raw.requiereBanco),
      permiteCambio: Boolean(raw.permiteCambio),
      orden: Number(raw.orden ?? 0),
      metadata
    } satisfies MetodoPagoUpdate;

    const id = this.editandoId();
    this.saving.set(true);
    const confirmado = await this.acciones.confirmar(id ? 'Editar' : 'Crear', { titulo: id ? 'Confirmar actualización' : 'Confirmar creación', mensaje: '¿Deseas guardar los cambios del método de pago?', confirmarTexto: 'Guardar' }).catch(() => false);
    if (!confirmado || !this.mostrandoFormulario() || !this.acciones.vigente(this.contextoEditor, id ? 'Editar' : 'Crear')) { this.saving.set(false); return; }
    const request$ = id
      ? this.service.update(id, base)
      : this.service.create({ ...base, codigo: raw.codigo!.trim().toUpperCase() } satisfies MetodoPagoCreate);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelar();
        this.snackBar.open('Método de pago guardado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargar();
      },
      error: err => {
        this.saving.set(false);
        this.error(err.error?.message ?? 'No se pudo guardar el método de pago.');
      }
    });
  }

  puedeCambiarEstado(metodo: MetodoPago): boolean {
    return metodo.activo ? this.puedeDesactivar() : this.puedeActivar();
  }

  async cambiarEstado(metodo: MetodoPago, evento: MatSlideToggleChange): Promise<void> {
    evento.source.checked = metodo.activo;
    if (!this.puedeCambiarEstado(metodo) || this.cambiandoEstado()) return;
    this.cambiandoEstado.set(true);
    try {
      const accion = metodo.activo ? 'Desactivar' : 'Activar';
      if (!await this.acciones.confirmar(accion, { titulo: accion + ' método', mensaje: '¿Deseas ' + accion.toLowerCase() + ' «' + metodo.nombre + '»?', confirmarTexto: accion }) || !this.permisos.puede('MetodosPago', accion)) return;
      const { data } = await firstValueFrom(metodo.activo ? this.service.desactivar(metodo.id) : this.service.activar(metodo.id));
      this.metodos.update(items => items.map(item => item.id === data.id ? data : item));
    } catch { this.error('No se pudo cambiar el estado del método de pago.'); }
    finally { this.cambiandoEstado.set(false); }
  }

  async eliminar(metodo: MetodoPago): Promise<void> {
    const confirmado = await this.acciones.confirmar('EliminarLogico', {
      titulo: 'Eliminar método de pago',
      mensaje: `Se ocultará “${metodo.nombre}” para nuevas operaciones sin borrar su historial relacionado.`,
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;

    this.service.delete(metodo.id).subscribe({
      next: () => {
        this.metodos.update(items => items.filter(item => item.id !== metodo.id));
        this.snackBar.open('Método de pago eliminado correctamente.', 'Cerrar', { duration: 3500 });
      },
      error: err => this.error(err.error?.message ?? 'No se pudo eliminar el método de pago.')
    });
  }

  async mover(indice: number, delta: -1 | 1): Promise<void> {
    if (this.reordering() || this.filtrado()) return;
    const destino = indice + delta;
    const actual = [...this.metodos()];
    if (destino < 0 || destino >= actual.length) return;

    this.reordering.set(true);
    const confirmado = await this.acciones.confirmar('Editar', { titulo: 'Confirmar orden', mensaje: '¿Deseas cambiar el orden de este método de pago?', confirmarTexto: 'Mover' }).catch(() => false);
    if (!confirmado || !this.permisos.puede('MetodosPago', 'Editar')) { this.reordering.set(false); return; }
    [actual[indice], actual[destino]] = [actual[destino], actual[indice]];
    const normalizados = actual.map((item, position) => ({ ...item, orden: position }));
    this.reordering.set(true);
    this.service.reordenar(normalizados.map(item => ({ id: item.id, orden: item.orden }))).subscribe({
      next: () => {
        this.metodos.set(normalizados);
        this.reordering.set(false);
      },
      error: err => {
        this.reordering.set(false);
        this.error(err.error?.message ?? 'No se pudo actualizar el orden.');
        this.cargar();
      }
    });
  }

  private normalizarMetadata(valor: string): string | null | undefined {
    const texto = valor.trim();
    if (!texto) {
      this.metadataError.set('');
      return null;
    }
    try {
      const parsed: unknown = JSON.parse(texto);
      if (parsed === null || Array.isArray(parsed) || typeof parsed !== 'object') {
        this.metadataError.set('Metadata debe ser un objeto JSON.');
        return undefined;
      }
      this.metadataError.set('');
      return JSON.stringify(parsed);
    } catch {
      this.metadataError.set('Metadata contiene JSON inválido.');
      return undefined;
    }
  }

  private siguienteOrden(): number {
    return this.metodos().reduce((max, item) => Math.max(max, item.orden), -1) + 1;
  }

  private error(mensaje: string): void {
    this.snackBar.open(mensaje, 'Cerrar', { duration: 5000 });
  }
}
