
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CuentaContableService } from '../../core/services/cuenta-contable.service';
import { CuentaContable, CuentaContableInput, TipoCuentaContable } from '../../core/models/cuenta-contable.model';

interface CuentaRow extends CuentaContable {
  depth: number;
}

@Component({
  selector: 'app-plan-cuentas',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
],
  template: `
    <main class="plan-cuentas" aria-labelledby="plan-cuentas-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Finanzas</p>
          <h1 id="plan-cuentas-title">Plan de cuentas</h1>
          <p class="subtitle">Administra la jerarquía contable sin crear catálogos especulativos.</p>
        </div>
        @if (puedeCrear()) {
          <button mat-flat-button color="primary" type="button" (click)="nuevo()">
            <mat-icon>add</mat-icon> Nueva cuenta
          </button>
        }
      </header>

      @if (errorMessage()) {
        <div class="alert error" role="alert">{{ errorMessage() }}</div>
      }
      @if (successMessage()) {
        <div class="alert success" role="status">{{ successMessage() }}</div>
      }

      @if (mostrarFormulario()) {
        <section class="card form-card" aria-labelledby="form-title">
          <div class="section-heading">
            <h2 id="form-title">{{ editingId() === null ? 'Nueva cuenta' : 'Editar cuenta' }}</h2>
            <button mat-button type="button" (click)="cancelar()">Cancelar</button>
          </div>
          <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
            <div class="form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Código</mat-label>
                <input matInput formControlName="codigo" autocomplete="off" />
                @if (form.controls.codigo.hasError('required')) { <mat-error>El código es obligatorio.</mat-error> }
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Nombre</mat-label>
                <input matInput formControlName="nombre" autocomplete="off" />
                @if (form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio.</mat-error> }
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Tipo</mat-label>
                <mat-select formControlName="tipo">
                  @for (tipo of tipos; track tipo.value) { <mat-option [value]="tipo.value">{{ tipo.label }}</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Cuenta padre</mat-label>
                <mat-select formControlName="cuentaPadreId">
                  <mat-option [value]="null">Sin cuenta padre</mat-option>
                  @for (parent of parentOptions(); track parent.id) {
                    <mat-option [value]="parent.id">{{ '—'.repeat(parent.depth) }} {{ parent.codigo }} · {{ parent.nombre }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Descripción</mat-label>
                <textarea matInput rows="3" formControlName="descripcion"></textarea>
              </mat-form-field>
            </div>
            <div class="checks">
              <mat-checkbox formControlName="aceptaMovimientos">Acepta movimientos directos</mat-checkbox>
              <mat-checkbox formControlName="activa">Cuenta activa</mat-checkbox>
            </div>
            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">
              @if (saving()) { <mat-spinner diameter="18"></mat-spinner> } @else { Guardar cuenta }
            </button>
          </form>
        </section>
      }

      <section class="card" aria-labelledby="tree-title">
        <div class="section-heading">
          <div>
            <h2 id="tree-title">Estructura contable</h2>
            <p class="muted">{{ rows().length }} cuentas visibles</p>
          </div>
          <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading()">
            <mat-icon>refresh</mat-icon> Actualizar
          </button>
        </div>

        @if (loading()) {
          <div class="loading" role="status"><mat-spinner diameter="36"></mat-spinner><span>Cargando cuentas…</span></div>
        } @else if (rows().length === 0) {
          <div class="empty">No hay cuentas contables registradas.</div>
        } @else {
          <div class="table-wrap">
            <table>
              <thead><tr><th>Código</th><th>Nombre</th><th>Tipo</th><th>Estado</th><th>Movimientos</th><th>Acciones</th></tr></thead>
              <tbody>
                @for (row of rows(); track row.id) {
                  <tr>
                    <td><span class="tree-code" [style.padding-left.rem]="row.depth * 1.25">{{ row.depth ? '↳ ' : '' }}{{ row.codigo }}</span></td>
                    <td>{{ row.nombre }}</td>
                    <td>{{ tipoLabel(row.tipo) }}</td>
                    <td><span class="status" [class.inactive]="!row.activa">{{ row.activa ? 'Activa' : 'Inactiva' }}</span></td>
                    <td>{{ row.aceptaMovimientos ? 'Sí' : 'No' }}</td>
                    <td>@if (puedeEditar()) { <button mat-icon-button type="button" (click)="editar(row)" [attr.aria-label]="'Editar ' + row.nombre" title="Editar"><mat-icon>edit</mat-icon></button> }</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>
    </main>
  `,
  styles: [`
    :host { display: block; }
    .plan-cuentas { max-width: 1180px; margin: 0 auto; padding: 24px; }
    .page-header, .section-heading { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
    .page-header { margin-bottom: 24px; }
    h1, h2, p { margin-top: 0; }
    h1 { margin-bottom: 6px; } h2 { margin-bottom: 4px; }
    .eyebrow { color: var(--color-primary, #075985); font-size: .8rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; margin-bottom: 6px; }
    .subtitle, .muted { color: var(--color-text-muted, #64748b); }
    .card { background: var(--color-surface, #fff); border: 1px solid var(--color-border, #d9e0e8); border-radius: 12px; padding: 20px; margin-bottom: 20px; }
    .form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px 16px; margin-top: 18px; }
    .full-width { grid-column: 1 / -1; }
    .checks { display: flex; flex-wrap: wrap; gap: 20px; margin: 2px 0 18px; }
    .alert { border-radius: 8px; padding: 12px 16px; margin-bottom: 16px; } .error { background: #fef2f2; color: #991b1b; } .success { background: #f0fdf4; color: #166534; }
    .loading, .empty { display: flex; align-items: center; justify-content: center; gap: 12px; min-height: 160px; color: var(--color-text-muted, #64748b); }
    .table-wrap { overflow-x: auto; } table { width: 100%; border-collapse: collapse; } th, td { padding: 12px 10px; border-bottom: 1px solid var(--color-border, #e2e8f0); text-align: left; white-space: nowrap; } th { font-size: .82rem; color: var(--color-text-muted, #64748b); }
    .tree-code { display: inline-block; min-width: 92px; font-variant-numeric: tabular-nums; } .status { color: #166534; font-weight: 600; } .status.inactive { color: #991b1b; }
    :host *:focus-visible { outline: 3px solid var(--color-primary, #0369a1); outline-offset: 2px; }
    @media (max-width: 700px) { .plan-cuentas { padding: 16px; } .page-header, .section-heading { align-items: flex-start; flex-direction: column; } .form-grid { grid-template-columns: 1fr; } .full-width { grid-column: auto; } }
  `]
})
export class PlanCuentasComponent implements OnInit {
  private readonly service = inject(CuentaContableService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly fb = inject(FormBuilder).nonNullable;

  readonly accounts = signal<CuentaContable[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly mostrarFormulario = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly puedeCrear = computed(() => this.permisos.puede('Finanzas', 'Crear'));
  readonly puedeEditar = computed(() => this.permisos.puede('Finanzas', 'Editar'));
  readonly rows = computed(() => this.flatten(this.accounts()));
  readonly parentOptions = computed(() => {
    const excluded = new Set<number>();
    if (this.editingId() !== null) this.collectDescendants(this.accounts(), this.editingId()!, excluded);
    return this.rows().filter(row => row.id !== this.editingId() && !excluded.has(row.id));
  });

  readonly tipos = [
    { value: TipoCuentaContable.Activo, label: 'Activo' },
    { value: TipoCuentaContable.Pasivo, label: 'Pasivo' },
    { value: TipoCuentaContable.Patrimonio, label: 'Patrimonio' },
    { value: TipoCuentaContable.Ingreso, label: 'Ingreso' },
    { value: TipoCuentaContable.Gasto, label: 'Gasto' },
    { value: TipoCuentaContable.Costo, label: 'Costo' }
  ];

  readonly form = this.fb.group({
    codigo: ['', [Validators.required, Validators.maxLength(50)]],
    nombre: ['', [Validators.required, Validators.maxLength(200)]],
    descripcion: ['', Validators.maxLength(1000)],
    tipo: [TipoCuentaContable.Activo, Validators.required],
    cuentaPadreId: [null as number | null],
    aceptaMovimientos: [true],
    activa: [true]
  });

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    if (!this.permisos.puede('Finanzas', 'Ver')) return;
    this.loading.set(true); this.errorMessage.set(null);
    this.service.getAll().pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => this.accounts.set(response.data ?? []),
      error: () => this.errorMessage.set('No se pudo cargar el plan de cuentas. Intenta nuevamente.')
    });
  }

  nuevo(): void {
    if (!this.puedeCrear()) return;
    this.editingId.set(null); this.form.reset({ codigo: '', nombre: '', descripcion: '', tipo: TipoCuentaContable.Activo, cuentaPadreId: null, aceptaMovimientos: true, activa: true });
    this.successMessage.set(null); this.errorMessage.set(null); this.mostrarFormulario.set(true);
  }

  editar(row: CuentaRow): void {
    if (!this.puedeEditar()) return;
    this.editingId.set(row.id); this.form.reset({ codigo: row.codigo, nombre: row.nombre, descripcion: row.descripcion ?? '', tipo: row.tipo, cuentaPadreId: row.cuentaPadreId, aceptaMovimientos: row.aceptaMovimientos, activa: row.activa });
    this.successMessage.set(null); this.errorMessage.set(null); this.mostrarFormulario.set(true);
  }

  cancelar(): void { this.mostrarFormulario.set(false); this.editingId.set(null); }

  guardar(): void {
    const id = this.editingId();
    if (this.form.invalid || (id === null && !this.puedeCrear()) || (id !== null && !this.puedeEditar())) { this.form.markAllAsTouched(); return; }
    const input = this.form.getRawValue() as CuentaContableInput;
    this.saving.set(true); this.errorMessage.set(null); this.successMessage.set(null);
    const request = id === null ? this.service.create(input) : this.service.update(id, input);
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => { this.successMessage.set(id === null ? 'Cuenta creada correctamente.' : 'Cuenta actualizada correctamente.'); this.mostrarFormulario.set(false); this.editingId.set(null); this.cargar(); },
      error: (error) => this.errorMessage.set(error?.error?.detail ?? error?.error?.message ?? 'La operación fue rechazada. Verifica los datos y la jerarquía.')
    });
  }

  tipoLabel(tipo: TipoCuentaContable): string { return this.tipos.find(item => item.value === tipo)?.label ?? 'Desconocido'; }

  private flatten(nodes: CuentaContable[], depth = 0): CuentaRow[] {
    return nodes.flatMap(node => [{ ...node, depth }, ...this.flatten(node.subcuentas ?? [], depth + 1)]);
  }

  private collectDescendants(nodes: CuentaContable[], id: number, output: Set<number>): boolean {
    for (const node of nodes) {
      if (node.id === id) { this.markDescendants(node.subcuentas ?? [], output); return true; }
      if (this.collectDescendants(node.subcuentas ?? [], id, output)) return true;
    }
    return false;
  }

  private markDescendants(nodes: CuentaContable[], output: Set<number>): void { for (const node of nodes) { output.add(node.id); this.markDescendants(node.subcuentas ?? [], output); } }
}
