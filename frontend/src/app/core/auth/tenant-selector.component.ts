
import { Component, EventEmitter, OnInit, Output, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { TenantContextService } from './tenant-context.service';

@Component({
  selector: 'app-tenant-selector',
  standalone: true,
  imports: [FormsModule],
  template: `
    <section class="tenant-card" aria-labelledby="tenant-title">
      <div class="tenant-icon" aria-hidden="true">🏢</div>
      <h1 id="tenant-title">Selecciona tu empresa</h1>
      <p class="tenant-help">
        El acceso se habilita únicamente después de que el servidor confirme tu membresía activa.
      </p>

      <form (ngSubmit)="activarEmpresa()" novalidate>
        <label for="empresa-id">ID de empresa</label>
        <input
          id="empresa-id"
          name="empresaId"
          type="number"
          min="1"
          step="1"
          inputmode="numeric"
          autocomplete="off"
          required
          [(ngModel)]="empresaId"
          [disabled]="cargando"
          [attr.aria-invalid]="error ? 'true' : null"
          [attr.aria-describedby]="error ? 'tenant-error' : 'tenant-help-inline'">
        <span id="tenant-help-inline" class="hint">Usa la empresa a la que tu usuario tiene acceso.</span>

        <button type="submit" [disabled]="cargando || !empresaValida()">
          {{ cargando ? 'Verificando…' : 'Entrar a la empresa' }}
        </button>
      </form>

      @if (cargando) {
        <p class="status" role="status" aria-live="polite">Verificando acceso con el servidor…</p>
      }
      @if (error) {
        <p id="tenant-error" class="error" role="alert">{{ error }}</p>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display: block; width: min(100%, 430px); }
    .tenant-card { background: #fff; border: 1px solid #e5e7eb; border-radius: 18px; padding: 2rem; box-shadow: 0 18px 45px rgba(15, 23, 42, .10); }
    .tenant-icon { font-size: 2rem; margin-bottom: .75rem; }
    h1 { margin: 0; color: #111827; font-size: 1.55rem; }
    .tenant-help { margin: .65rem 0 1.5rem; color: #4b5563; line-height: 1.5; }
    form { display: grid; gap: .65rem; }
    label { font-weight: 650; color: #1f2937; }
    input { min-height: 44px; border: 1px solid #9ca3af; border-radius: 10px; padding: 0 .8rem; font: inherit; }
    input:focus { outline: 3px solid rgba(37, 99, 235, .25); border-color: #2563eb; }
    .hint { color: #6b7280; font-size: .875rem; }
    button { min-height: 44px; margin-top: .55rem; border: 0; border-radius: 10px; padding: 0 1rem; font: inherit; font-weight: 700; background: #1d4ed8; color: #fff; cursor: pointer; }
    button:disabled { opacity: .6; cursor: not-allowed; }
    .status { margin: 1rem 0 0; color: #374151; }
    .error { margin: 1rem 0 0; color: #b91c1c; font-weight: 600; }
  `]
})
export class TenantSelectorComponent implements OnInit {
  @Output() readonly confirmado = new EventEmitter<void>();

  empresaId: number | null;
  cargando = false;
  error = '';

  constructor(public readonly tenant: TenantContextService) {
    this.empresaId = this.tenant.empresaSolicitadaId();
  }

  ngOnInit(): void {
    // Una selección persistida sigue siendo sólo una solicitud. Revalidarla
    // automáticamente conserva continuidad sin convertir localStorage en autoridad.
    if (this.empresaValida()) {
      this.activarEmpresa();
    }
  }

  empresaValida(): boolean {
    return Number.isInteger(this.empresaId) && Number(this.empresaId) > 0;
  }

  activarEmpresa(): void {
    if (!this.empresaValida() || this.cargando) {
      this.error = 'Ingresa un identificador de empresa válido.';
      return;
    }

    this.error = '';
    this.cargando = true;
    this.tenant.seleccionarEmpresa(Number(this.empresaId))
      .pipe(finalize(() => { this.cargando = false; }))
      .subscribe({
        next: () => this.confirmado.emit(),
        error: () => {
          this.error = 'No fue posible validar el acceso a esta empresa. Verifica tu membresía e inténtalo nuevamente.';
        }
      });
  }
}
