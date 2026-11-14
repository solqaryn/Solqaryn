
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AutomatizacionService } from '../../services/automatizacion.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-automatizacion-configuracion-card',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule],
  template: `
    <section class="card automation-settings" aria-labelledby="m12-config-title">
      <div class="heading">
        <div><h2 id="m12-config-title">Automatización transversal</h2><p>Umbrales deterministas para recordatorios, sugerencias y autocompletado. Ninguna regla modifica inventario o finanzas automáticamente.</p></div>
        @if (version()) { <span>{{ version() }}</span> }
      </div>
      @if (loading()) { <div class="loading"><mat-spinner diameter="32"></mat-spinner></div> }
      @else {
        <form [formGroup]="form" (ngSubmit)="guardar()">
          <div class="grid">
            <mat-form-field appearance="outline"><mat-label>Días alerta venta en borrador</mat-label><input matInput type="number" min="1" max="90" formControlName="diasBorradorVentaAlerta"></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Días alerta compra en borrador</mat-label><input matInput type="number" min="1" max="180" formControlName="diasBorradorCompraAlerta"></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Días alerta carga pendiente</mat-label><input matInput type="number" min="1" max="30" formControlName="diasCargaPendienteAlerta"></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Días alerta finanzas pendientes</mat-label><input matInput type="number" min="1" max="180" formControlName="diasMovimientoFinancieroPendienteAlerta"></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Máximo de sugerencias</mat-label><input matInput type="number" min="5" max="100" formControlName="limiteSugerencias"></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Máximo de autocompletado</mat-label><input matInput type="number" min="5" max="50" formControlName="limiteAutocompletado"></mat-form-field>
          </div>
          <label class="toggle"><input type="checkbox" formControlName="mostrarRecordatoriosDashboard"> Mostrar recordatorios operativos en Dashboard</label>
          <p class="hint">Las acciones masivas M12 son únicamente vistas previas y siempre requieren confirmación mediante el flujo transaccional correspondiente.</p>
          @if (!puedeEditar()) { <p class="readonly"><mat-icon>info</mat-icon> Necesitas “Configuración: Editar” para modificar estos valores.</p> }
          @if (error()) { <p class="error">{{ error() }}</p> }
          @if (puedeEditar()) {
            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving()">
              @if (saving()) { <mat-spinner diameter="18"></mat-spinner> } @else { <mat-icon>automation</mat-icon> }
              Guardar automatización
            </button>
          }
        </form>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .automation-settings{margin:24px 0;padding:24px}.heading{display:flex;justify-content:space-between;gap:16px;align-items:flex-start}.heading h2{margin:0 0 6px}.heading p{margin:0;max-width:760px}.heading>span{font-weight:700}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:12px;margin-top:20px}.toggle{display:flex;gap:10px;align-items:center;margin:4px 0 14px}.hint{opacity:.78}.readonly{display:flex;gap:8px;align-items:center}.error{color:var(--color-danger)}button{min-height:44px}.loading{display:flex;justify-content:center;padding:24px}@media(max-width:600px){.automation-settings{padding:18px}.heading{flex-direction:column}}
  `]
})
export class AutomatizacionConfiguracionCardComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly version = signal('');
  readonly puedeEditar = signal(false);

  readonly form = this.fb.group({
    diasBorradorVentaAlerta: [2, [Validators.required, Validators.min(1), Validators.max(90)]],
    diasBorradorCompraAlerta: [7, [Validators.required, Validators.min(1), Validators.max(180)]],
    diasCargaPendienteAlerta: [1, [Validators.required, Validators.min(1), Validators.max(30)]],
    diasMovimientoFinancieroPendienteAlerta: [7, [Validators.required, Validators.min(1), Validators.max(180)]],
    limiteSugerencias: [20, [Validators.required, Validators.min(5), Validators.max(100)]],
    limiteAutocompletado: [10, [Validators.required, Validators.min(5), Validators.max(50)]],
    mostrarRecordatoriosDashboard: [true]
  });

  constructor(private readonly service: AutomatizacionService, private readonly permisos: PermisosRuntimeService, private readonly snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.puedeEditar.set(this.permisos.puede('Configuracion', 'Editar'));
    this.service.getConfiguracion().subscribe({
      next: (res) => {
        this.form.patchValue(res.data);
        this.version.set(res.data.versionReglas);
        if (!this.puedeEditar()) this.form.disable();
        this.loading.set(false);
      },
      error: (err) => { this.error.set(err.error?.message ?? 'No se pudo cargar la automatización.'); this.loading.set(false); }
    });
  }

  guardar(): void {
    if (!this.puedeEditar() || this.form.invalid || this.saving()) return;
    this.saving.set(true); this.error.set(null);
    const v = this.form.getRawValue();
    this.service.updateConfiguracion({
      diasBorradorVentaAlerta: Number(v.diasBorradorVentaAlerta), diasBorradorCompraAlerta: Number(v.diasBorradorCompraAlerta),
      diasCargaPendienteAlerta: Number(v.diasCargaPendienteAlerta), diasMovimientoFinancieroPendienteAlerta: Number(v.diasMovimientoFinancieroPendienteAlerta),
      limiteSugerencias: Number(v.limiteSugerencias), limiteAutocompletado: Number(v.limiteAutocompletado), mostrarRecordatoriosDashboard: !!v.mostrarRecordatoriosDashboard
    }).subscribe({
      next: (res) => { this.version.set(res.data.versionReglas); this.saving.set(false); this.snackBar.open('Automatización transversal actualizada.', 'Cerrar', { duration: 3500 }); },
      error: (err) => { this.error.set(err.error?.message ?? 'No se pudo guardar la automatización.'); this.saving.set(false); }
    });
  }
}
