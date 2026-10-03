
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { SecuenciaDocumentoConsulta } from '../../core/models/secuencia-documento.model';
import { SecuenciaDocumentoService } from '../../services/secuencia-documento.service';

@Component({
  selector: 'app-secuencia-documento-card',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
],
  template: `
    <section class="card numeracion-card" aria-labelledby="numeracion-title">
      <div class="header-row">
        <div>
          <h2 id="numeracion-title">Numeraciones de documentos</h2>
          <p class="subtitle">Consulta y reserva secuencias independientes por empresa, sucursal y tipo documental.</p>
        </div>
        @if (tenantId() !== null) {
          <span class="tenant-chip">Empresa {{ tenantId() }}</span>
        }
      </div>

      @if (tenantId() === null) {
        <p class="notice error" role="alert">
          <mat-icon>lock</mat-icon>
          Selecciona y valida una empresa antes de administrar numeraciones.
        </p>
      } @else {
        <form [formGroup]="form" class="query-grid" (ngSubmit)="consultar()">
          <mat-form-field appearance="outline">
            <mat-label>Tipo de documento</mat-label>
            <input matInput formControlName="tipoDocumento" placeholder="FACTURA" autocomplete="off">
            @if (form.controls.tipoDocumento.hasError('required')) {
              <mat-error>El tipo documental es obligatorio.</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Sucursal (opcional)</mat-label>
            <input matInput type="number" min="1" step="1" formControlName="sucursalId" placeholder="Todas / empresa">
            @if (form.controls.sucursalId.hasError('min')) {
              <mat-error>La sucursal debe ser mayor que cero.</mat-error>
            }
          </mat-form-field>

          <button mat-stroked-button type="submit" [disabled]="form.invalid || loading()">
            @if (loading()) { <mat-spinner diameter="18"></mat-spinner> } @else { <mat-icon>search</mat-icon> }
            Consultar
          </button>
        </form>

        @if (errorMessage()) {
          <p class="notice error" role="alert">{{ errorMessage() }}</p>
        }

        @if (secuencia(); as item) {
          <div class="sequence-summary" aria-live="polite">
            <div><span>Tipo</span><strong>{{ item.tipoDocumento }}</strong></div>
            <div><span>Ámbito</span><strong>{{ item.sucursalId ? ('Sucursal ' + item.sucursalId) : 'Empresa' }}</strong></div>
            <div><span>Prefijo</span><strong>{{ item.prefijo || '—' }}</strong></div>
            <div><span>Último valor</span><strong>{{ item.ultimoValor }}</strong></div>
            <div><span>Último número</span><strong>{{ item.ultimoNumeroFormateado || 'Sin reservas' }}</strong></div>
            <div><span>Estado</span><strong>{{ item.activa ? 'Activa' : 'Inactiva' }}</strong></div>
          </div>

          @if (puedeEditar()) {
            <div class="actions">
              <button mat-flat-button color="primary" type="button" (click)="reservarSiguiente()" [disabled]="reserving() || !item.activa">
                @if (reserving()) { <mat-spinner diameter="18"></mat-spinner> } @else { <mat-icon>confirmation_number</mat-icon> }
                Reservar siguiente número
              </button>
            </div>
          } @else {
            <p class="readonly-hint"><mat-icon>info</mat-icon> Solo lectura: necesitas “Configuración: Editar” para reservar números.</p>
          }
        } @else if (!loading() && consulted()) {
          <p class="empty-state">No se encontró una secuencia para el ámbito solicitado.</p>
        }
      }
    </section>
  `,
  styles: [`
    .numeracion-card { margin-top: 1.5rem; padding: 1.25rem; }
    .header-row { display:flex; justify-content:space-between; align-items:flex-start; gap:1rem; flex-wrap:wrap; }
    .header-row h2 { margin:0; }
    .subtitle { margin:.35rem 0 0; opacity:.75; }
    .tenant-chip { padding:.35rem .7rem; border-radius:999px; background:rgba(0,0,0,.06); font-weight:600; }
    .query-grid { display:grid; grid-template-columns:minmax(220px,2fr) minmax(180px,1fr) auto; gap:1rem; align-items:center; margin-top:1rem; }
    .query-grid button { min-height:48px; }
    .sequence-summary { display:grid; grid-template-columns:repeat(3,minmax(0,1fr)); gap:.75rem; margin-top:1rem; }
    .sequence-summary div { padding:.85rem; border:1px solid rgba(0,0,0,.1); border-radius:8px; display:flex; flex-direction:column; gap:.25rem; }
    .sequence-summary span { font-size:.8rem; opacity:.7; }
    .notice, .readonly-hint, .empty-state { display:flex; align-items:center; gap:.5rem; margin-top:1rem; }
    .error { color:#b42318; }
    .actions { display:flex; justify-content:flex-end; margin-top:1rem; }
    @media (max-width: 760px) {
      .query-grid, .sequence-summary { grid-template-columns:1fr; }
      .query-grid button { width:100%; }
    }
  `]
})
export class SecuenciaDocumentoCardComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(SecuenciaDocumentoService);
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly snackBar = inject(MatSnackBar);

  readonly loading = signal(false);
  readonly reserving = signal(false);
  readonly consulted = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly secuencia = signal<SecuenciaDocumentoConsulta | null>(null);
  readonly puedeEditar = signal(false);
  readonly tenantId = this.tenant.empresaIdVerificada;

  readonly form = this.fb.group({
    tipoDocumento: ['FACTURA', [Validators.required, Validators.maxLength(80)]],
    sucursalId: [null as number | null, [Validators.min(1)]]
  });

  ngOnInit(): void {
    this.puedeEditar.set(this.permisos.puede('Configuracion', 'Editar'));
  }

  consultar(): void {
    const empresaId = this.tenantId();
    if (empresaId === null || this.form.invalid) {
      this.secuencia.set(null);
      return;
    }

    const tipoDocumento = this.form.controls.tipoDocumento.value?.trim() ?? '';
    const sucursalId = this.normalizarSucursal(this.form.controls.sucursalId.value);

    this.loading.set(true);
    this.consulted.set(true);
    this.errorMessage.set(null);
    this.secuencia.set(null);

    this.service.obtener(empresaId, sucursalId, tipoDocumento).subscribe({
      next: (response) => {
        this.loading.set(false);
        this.secuencia.set(response.data ?? null);
      },
      error: (error) => {
        this.loading.set(false);
        this.errorMessage.set(error.error?.message ?? 'No se pudo consultar la numeración.');
      }
    });
  }

  reservarSiguiente(): void {
    const empresaId = this.tenantId();
    const item = this.secuencia();
    if (empresaId === null || !item || !this.puedeEditar() || this.reserving()) return;

    this.reserving.set(true);
    this.errorMessage.set(null);

    this.service.reservarSiguiente({
      empresaId,
      sucursalId: item.sucursalId,
      tipoDocumento: item.tipoDocumento
    }).subscribe({
      next: (response) => {
        this.reserving.set(false);
        this.snackBar.open(`Número reservado: ${response.data.numero}`, 'Cerrar', { duration: 5000 });
        this.consultar();
      },
      error: (error) => {
        this.reserving.set(false);
        this.errorMessage.set(error.error?.message ?? 'No se pudo reservar el siguiente número.');
      }
    });
  }

  private normalizarSucursal(value: number | null): number | null {
    if (value === null || value === undefined || value === ('' as unknown as number)) return null;
    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  }
}
