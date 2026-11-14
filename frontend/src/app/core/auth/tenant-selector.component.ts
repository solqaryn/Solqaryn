
import { Component, DestroyRef, EventEmitter, OnInit, Output, ChangeDetectionStrategy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TimeoutError, finalize } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
        <label for="empresa-id">Empresa autorizada</label>
        <select
          id="empresa-id"
          name="empresaId"
          required
          [(ngModel)]="empresaId"
          [disabled]="cargando || cargandoEmpresas || empresas.length === 0"
          [attr.aria-invalid]="error ? 'true' : null"
          aria-describedby="tenant-help-inline">
          <option [ngValue]="null" disabled>Selecciona tu empresa</option>
          @for (empresa of empresas; track empresa.empresaId) {
            <option [ngValue]="empresa.empresaId">{{ empresa.nombre }}</option>
          }
        </select>
        <span id="tenant-help-inline" class="hint">Solo se muestran las empresas de tus membresías activas.</span>

        <button type="submit" [disabled]="cargando || cargandoEmpresas || !empresaValida()">
          {{ cargando ? 'Verificando…' : 'Entrar a la empresa' }}
        </button>
      </form>
      @if (cargandoEmpresas) {
        <p class="status" role="status" aria-live="polite">Buscando tus empresas autorizadas…</p>
      }
      @if (!cargandoEmpresas && empresas.length === 0 && !errorLista) {
        <p class="status" role="status">No tienes membresías empresariales activas disponibles.</p>
      }
      @if (errorLista && !cargandoEmpresas) {
        <button class="retry" type="button" (click)="cargarEmpresas()">Volver a consultar empresas</button>
      }

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
    select { min-height: 44px; width: 100%; border: 1px solid #9ca3af; border-radius: 10px; padding: 0 .8rem; font: inherit; background: #fff; color: #111827; }
    select:focus-visible { outline: 3px solid rgba(37, 99, 235, .25); border-color: #2563eb; }
    .hint { color: #6b7280; font-size: .875rem; }
    button { min-height: 44px; margin-top: .55rem; border: 0; border-radius: 10px; padding: 0 1rem; font: inherit; font-weight: 700; background: #1d4ed8; color: #fff; cursor: pointer; }
    button:disabled { opacity: .6; cursor: not-allowed; }
    .retry { background: #334155; margin-top: .75rem; }
    .status { margin: 1rem 0 0; color: #374151; }
    .error { margin: 1rem 0 0; color: #b91c1c; font-weight: 600; }
  `]
})
export class TenantSelectorComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  @Output() readonly confirmado = new EventEmitter<void>();

  empresaId: number | null;
  cargando = false;
  cargandoEmpresas = false;
  errorLista = false;
  empresas: { empresaId: number; nombre: string }[] = [];
  error = '';

  constructor(public readonly tenant: TenantContextService) {
    this.empresaId = this.tenant.empresaSolicitadaId();
  }

  ngOnInit(): void {
    this.cargarEmpresas();
  }

  cargarEmpresas(): void {
    if (this.cargandoEmpresas) return;
    this.cargandoEmpresas = true;
    this.errorLista = false;
    this.error = '';

    this.tenant.listarEmpresasSeleccionables()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => { this.cargandoEmpresas = false; })
      )
      .subscribe({
        next: empresas => {
          this.empresas = empresas;
          // El ID recordado no tiene autoridad ni puede seleccionar una empresa
          // que ya no está en la lista vigente del actor JWT.
          if (!this.empresaValida()) this.empresaId = null;
          if (this.empresaValida()) this.activarEmpresa();
        },
        error: () => {
          this.empresas = [];
          this.empresaId = null;
          this.errorLista = true;
          this.error = 'No pudimos consultar tus empresas autorizadas. Inténtalo de nuevo.';
        }
      });
  }

  empresaValida(): boolean {
    return Number.isInteger(this.empresaId) &&
      this.empresas.some(empresa => empresa.empresaId === Number(this.empresaId));
  }

  activarEmpresa(): void {
    if (!this.empresaValida() || this.cargando) {
      this.error = 'Selecciona una empresa de tus membresías activas.';
      return;
    }

    this.error = '';
    this.cargando = true;
    this.tenant.seleccionarEmpresa(Number(this.empresaId))
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => { this.cargando = false; })
      )
      .subscribe({
        next: () => this.confirmado.emit(),
        error: (err: unknown) => {
          const status = typeof err === 'object' && err !== null && 'status' in err
            ? Number(err.status) : null;
          this.error = err instanceof TimeoutError
            ? 'El servidor tardó demasiado en confirmar tu membresía. Inténtalo nuevamente o entra a la consola global.'
            : status === 403
              ? 'Esta cuenta no tiene membresía empresarial activa para el ID indicado. Selecciona otra empresa o entra a la consola global.'
              : 'No fue posible validar el acceso a esta empresa. Verifica tu membresía e inténtalo nuevamente.';
        }
      });
  }
}
