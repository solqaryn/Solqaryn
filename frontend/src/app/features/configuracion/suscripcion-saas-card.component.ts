import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, finalize, forkJoin, map, of } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { LimiteSuscripcionSaaS, SuscripcionSaaS } from '../../core/models/suscripcion-saas.model';
import { SuscripcionSaaSService } from '../../services/suscripcion-saas.service';

interface ModuloSaaSUx {
  clave: string;
  nombre: string;
  descripcion: string;
  habilitado: boolean;
  motivo: number | string;
}

const MODULOS_SAAS: ReadonlyArray<Pick<ModuloSaaSUx, 'clave' | 'nombre' | 'descripcion'>> = [
  { clave: 'INVENTARIO', nombre: 'Inventario', descripcion: 'Existencias, almacenes y movimientos.' },
  { clave: 'VENTAS', nombre: 'Ventas', descripcion: 'Pedidos, facturación y cobros.' },
  { clave: 'COMPRAS', nombre: 'Compras', descripcion: 'Solicitudes, órdenes y recepciones.' },
  { clave: 'REPORTES', nombre: 'Reportes', descripcion: 'Indicadores y reportes operativos.' }
];

@Component({
  selector: 'app-suscripcion-saas-card',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  template: `
    <section class="card saas-card" aria-labelledby="saas-title">
      <div class="header-row">
        <div>
          <h2 id="saas-title">Plan y suscripción</h2>
          <p class="hint">Consulta el plan, los límites y la disponibilidad de módulos del tenant verificado.</p>
        </div>
        @if (empresaId()) {
          <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading() || onboarding()">
            <mat-icon>refresh</mat-icon> Actualizar
          </button>
        }
      </div>

      @if (!empresaId()) {
        <div class="state warning" role="alert">
          <mat-icon>domain_disabled</mat-icon>
          <span>Selecciona una empresa autorizada para consultar su suscripción.</span>
        </div>
      } @else if (loading()) {
        <div class="state" role="status" aria-live="polite">
          <mat-spinner diameter="32"></mat-spinner><span>Cargando plan y límites…</span>
        </div>
      } @else if (error()) {
        <div class="state error" role="alert">
          <mat-icon>error_outline</mat-icon><span>{{ error() }}</span>
          <button mat-button type="button" (click)="cargar()">Reintentar</button>
        </div>
      } @else if (suscripcion()) {
        <article class="subscription" aria-label="Suscripción actual">
          <div>
            <span class="eyebrow">Plan actual</span>
            <h3>{{ suscripcion()!.planNombre }}</h3>
            <p class="plan-code">{{ suscripcion()!.planCodigo }}</p>
          </div>
          <dl>
            <div><dt>Estado</dt><dd>{{ estadoLegible(suscripcion()!.estado) }}</dd></div>
            <div><dt>Inicio</dt><dd>{{ suscripcion()!.inicioUtc | date:'dd/MM/yyyy HH:mm' }}</dd></div>
            <div><dt>Fin</dt><dd>{{ suscripcion()!.finUtc ? (suscripcion()!.finUtc | date:'dd/MM/yyyy HH:mm') : 'Sin fecha definida' }}</dd></div>
          </dl>
        </article>

        <div class="modules-header">
          <div>
            <h3>Módulos del plan</h3>
            <p class="hint">El acceso se decide en el backend para este tenant. Los módulos no incluidos quedan deshabilitados.</p>
          </div>
        </div>

        @if (loadingModulos()) {
          <div class="state compact" role="status" aria-live="polite">
            <mat-spinner diameter="28"></mat-spinner><span>Verificando módulos del plan…</span>
          </div>
        } @else {
          <div class="modules-grid" aria-label="Disponibilidad de módulos">
            @for (modulo of modulos(); track modulo.clave) {
              <article class="module-tile" [class.disabled]="!modulo.habilitado">
                <div class="module-title">
                  <mat-icon>{{ modulo.habilitado ? 'verified' : 'lock' }}</mat-icon>
                  <div><strong>{{ modulo.nombre }}</strong><p>{{ modulo.descripcion }}</p></div>
                </div>
                <button mat-stroked-button type="button" [disabled]="!modulo.habilitado" [attr.aria-label]="mensajeModulo(modulo)">
                  {{ modulo.habilitado ? 'Incluido' : 'No incluido' }}
                </button>
                @if (!modulo.habilitado) {
                  <small role="status">{{ mensajeModulo(modulo) }}</small>
                }
              </article>
            }
          </div>
        }

        <div class="limits-header">
          <div><h3>Límites del plan</h3><p class="hint">{{ totalLimites() }} límite(s) configurado(s).</p></div>
          <form [formGroup]="filtroForm" (ngSubmit)="cargarLimites()" class="filter" aria-label="Filtrar límites">
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Clave</mat-label>
              <input matInput formControlName="clave" maxlength="100" placeholder="USUARIOS" />
            </mat-form-field>
            <button mat-stroked-button type="submit" [disabled]="loadingLimites()">
              @if (loadingLimites()) { <mat-spinner diameter="18"></mat-spinner> } @else { <mat-icon>search</mat-icon> }
              Filtrar
            </button>
          </form>
        </div>

        @if (loadingLimites()) {
          <div class="state compact" role="status" aria-live="polite"><mat-spinner diameter="28"></mat-spinner><span>Cargando límites…</span></div>
        } @else if (limites().length === 0) {
          <div class="state compact" role="status"><mat-icon>rule</mat-icon><span>No hay límites para el filtro indicado.</span></div>
        } @else {
          <div class="table-wrap">
            <table>
              <thead><tr><th>Función</th><th>Valor máximo</th></tr></thead>
              <tbody>
                @for (limite of limites(); track limite.clave) {
                  <tr><td><strong>{{ limite.clave }}</strong></td><td>{{ limite.valorMaximo ?? 'Sin límite' }}</td></tr>
                }
              </tbody>
            </table>
          </div>
        }
      } @else {
        <div class="empty" role="status">
          <mat-icon>workspace_premium</mat-icon>
          <div><h3>Este tenant aún no tiene una suscripción</h3><p class="hint">Inicia el onboarding con el código de un plan activo.</p></div>
        </div>

        @if (puedeCrear()) {
          <form [formGroup]="onboardingForm" (ngSubmit)="iniciarOnboarding()" class="onboarding-form" aria-label="Onboarding de suscripción">
            <mat-form-field appearance="outline">
              <mat-label>Código del plan</mat-label>
              <input matInput formControlName="planCodigo" maxlength="80" autocomplete="off" />
              @if (onboardingForm.controls.planCodigo.hasError('required')) { <mat-error>El código del plan es obligatorio.</mat-error> }
              @if (onboardingForm.controls.planCodigo.hasError('whitespace')) { <mat-error>El código no puede contener sólo espacios.</mat-error> }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Inicio</mat-label>
              <input matInput type="datetime-local" formControlName="inicioLocal" />
            </mat-form-field>
            <button mat-flat-button color="primary" type="submit" [disabled]="onboardingForm.invalid || onboarding()">
              @if (onboarding()) { <mat-spinner diameter="18"></mat-spinner> } @else { <mat-icon>rocket_launch</mat-icon> }
              Iniciar suscripción
            </button>
          </form>
        } @else {
          <p class="readonly"><mat-icon>lock</mat-icon> Necesitas el permiso “Configuración: Crear” para iniciar una suscripción.</p>
        }
      }
    </section>
  `,
  styles: [`
    .saas-card{margin-top:1.25rem;padding:1.25rem}.header-row,.limits-header,.modules-header,.filter,.state,.empty,.onboarding-form,.readonly{display:flex;align-items:center;gap:.75rem}.header-row,.limits-header,.modules-header{justify-content:space-between;flex-wrap:wrap}h2,h3,p{margin-top:0}.hint{margin:.25rem 0;opacity:.75}.state{min-height:120px;justify-content:center;text-align:center}.state.compact{min-height:80px}.state.error{color:var(--color-error,#b3261e);flex-wrap:wrap}.warning{color:#8a4b00}.subscription{display:grid;grid-template-columns:minmax(180px,.8fr) minmax(280px,1.2fr);gap:1.25rem;margin:1rem 0;padding:1rem;border:1px solid rgba(0,0,0,.12);border-radius:10px}.eyebrow{font-size:.75rem;text-transform:uppercase;letter-spacing:.08em;opacity:.7}.plan-code{font-family:monospace;margin:0}.subscription dl{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:.75rem;margin:0}.subscription dl div{padding:.65rem;background:rgba(0,0,0,.035);border-radius:8px}.subscription dt{font-size:.75rem;opacity:.7}.subscription dd{margin:.25rem 0 0;font-weight:600}.modules-header,.limits-header{margin-top:1.25rem}.modules-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:.75rem}.module-tile{padding:.9rem;border:1px solid rgba(0,0,0,.14);border-radius:10px;display:grid;gap:.7rem}.module-tile.disabled{background:rgba(0,0,0,.035);opacity:.78}.module-title{display:flex;align-items:flex-start;gap:.6rem}.module-title p{margin:.2rem 0 0;font-size:.85rem;opacity:.75}.module-tile small{line-height:1.35}.filter{flex-wrap:wrap}.filter mat-form-field{width:min(240px,100%)}.table-wrap{overflow-x:auto}table{width:100%;border-collapse:collapse;min-width:420px}th,td{padding:.75rem;border-bottom:1px solid rgba(0,0,0,.12);text-align:left}.empty{justify-content:center;padding:1.25rem;text-align:left}.empty>mat-icon{font-size:2rem;width:2rem;height:2rem}.onboarding-form{align-items:flex-start;flex-wrap:wrap;justify-content:center}.onboarding-form mat-form-field{width:min(280px,100%)}.onboarding-form button{min-height:56px}.readonly{width:fit-content;margin:1rem auto;padding:.75rem 1rem;border:1px solid rgba(0,0,0,.12);border-radius:8px}@media(max-width:760px){.subscription{grid-template-columns:1fr}.subscription dl{grid-template-columns:1fr}.header-row>button,.filter,.filter mat-form-field,.filter button,.onboarding-form,.onboarding-form mat-form-field,.onboarding-form button{width:100%}.limits-header{align-items:stretch}}
  `]
})
export class SuscripcionSaaSCardComponent implements OnInit {
  private readonly service = inject(SuscripcionSaaSService);
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private initialized = false;
  private loadGeneration = 0;
  private pendingKey: string | null = null;
  private pendingPayload: string | null = null;

  readonly empresaId = this.tenant.empresaIdVerificada;
  readonly suscripcion = signal<SuscripcionSaaS | null>(null);
  readonly limites = signal<LimiteSuscripcionSaaS[]>([]);
  readonly modulos = signal<ModuloSaaSUx[]>([]);
  readonly totalLimites = signal(0);
  readonly loading = signal(true);
  readonly loadingLimites = signal(false);
  readonly loadingModulos = signal(false);
  readonly onboarding = signal(false);
  readonly error = signal<string | null>(null);
  readonly puedeCrear = signal(false);

  readonly filtroForm = this.fb.group({ clave: [''] });
  readonly onboardingForm = this.fb.group({
    planCodigo: ['', [Validators.required, Validators.maxLength(80), this.noWhitespaceValidator]],
    inicioLocal: [this.ahoraLocal(), Validators.required]
  });

  private readonly tenantWatcher = effect(() => {
    this.empresaId();
    if (this.initialized) this.cargar();
  });

  ngOnInit(): void {
    this.initialized = true;
    this.puedeCrear.set(this.permisos.puede('Configuracion', 'Crear'));
    this.cargar();
  }

  cargar(): void {
    const empresaId = this.empresaId();
    const generation = ++this.loadGeneration;
    this.error.set(null);
    this.suscripcion.set(null);
    this.limites.set([]);
    this.modulos.set([]);
    this.totalLimites.set(0);

    if (!empresaId) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.service.obtenerActual(empresaId).pipe(finalize(() => {
      if (generation === this.loadGeneration) this.loading.set(false);
    })).subscribe({
      next: res => {
        if (generation !== this.loadGeneration || empresaId !== this.empresaId()) return;
        this.suscripcion.set(res.data);
        this.cargarLimites();
        this.cargarModulos();
      },
      error: (err: HttpErrorResponse) => {
        if (generation !== this.loadGeneration || empresaId !== this.empresaId()) return;
        if (err.status === 404) return;
        this.error.set(this.mensajeError(err, 'No se pudo cargar la suscripción del tenant.'));
      }
    });
  }

  cargarModulos(): void {
    const empresaId = this.empresaId();
    const suscripcionId = this.suscripcion()?.id;
    if (!empresaId || !suscripcionId) {
      this.modulos.set([]);
      return;
    }

    this.loadingModulos.set(true);
    const consultas = MODULOS_SAAS.map(def =>
      this.service.obtenerEntitlementModulo(empresaId, def.clave).pipe(
        map(res => ({ ...def, habilitado: !!res.data?.habilitado, motivo: res.data?.motivo ?? 'SIN_DECISION' })),
        catchError(() => of({ ...def, habilitado: false, motivo: 'NO_DISPONIBLE' as const }))
      )
    );

    forkJoin(consultas).pipe(finalize(() => this.loadingModulos.set(false))).subscribe(resultados => {
      if (empresaId !== this.empresaId() || suscripcionId !== this.suscripcion()?.id) return;
      this.modulos.set(resultados);
    });
  }

  cargarLimites(): void {
    const empresaId = this.empresaId();
    if (!empresaId || !this.suscripcion()) return;
    const expectedSubscriptionId = this.suscripcion()!.id;
    this.loadingLimites.set(true);
    this.error.set(null);
    this.service.obtenerLimites(empresaId, this.filtroForm.controls.clave.value ?? undefined)
      .pipe(finalize(() => this.loadingLimites.set(false)))
      .subscribe({
        next: res => {
          if (empresaId !== this.empresaId() || expectedSubscriptionId !== this.suscripcion()?.id) return;
          this.limites.set(res.data?.items ?? []);
          this.totalLimites.set(res.data?.total ?? 0);
        },
        error: err => {
          if (empresaId !== this.empresaId()) return;
          this.limites.set([]);
          this.totalLimites.set(0);
          this.error.set(this.mensajeError(err, 'No se pudieron cargar los límites del plan.'));
        }
      });
  }

  iniciarOnboarding(): void {
    const empresaId = this.empresaId();
    const planCodigo = this.onboardingForm.controls.planCodigo.value?.trim().toUpperCase() ?? '';
    const inicioLocal = this.onboardingForm.controls.inicioLocal.value;
    if (!empresaId || !this.puedeCrear() || !planCodigo || !inicioLocal || this.onboardingForm.invalid) {
      this.onboardingForm.markAllAsTouched();
      return;
    }

    const request = { planCodigo, inicioUtc: new Date(inicioLocal).toISOString() };
    const payload = JSON.stringify(request);
    if (payload !== this.pendingPayload) {
      this.pendingPayload = payload;
      this.pendingKey = this.nuevaIdempotencyKey();
    }

    this.onboarding.set(true);
    this.error.set(null);
    this.service.onboarding(empresaId, request, this.pendingKey!).pipe(finalize(() => this.onboarding.set(false))).subscribe({
      next: res => {
        if (empresaId !== this.empresaId()) return;
        this.pendingKey = null;
        this.pendingPayload = null;
        this.suscripcion.set(res.data);
        this.snackBar.open('Suscripción iniciada correctamente.', 'Cerrar', { duration: 4000 });
        this.cargarLimites();
        this.cargarModulos();
      },
      error: err => {
        if (empresaId !== this.empresaId()) return;
        this.error.set(this.mensajeError(err, 'No se pudo iniciar la suscripción. Puedes reintentar de forma segura.'));
      }
    });
  }

  estadoLegible(estado: number | string): string {
    if (estado === 1 || String(estado).toLowerCase() === 'activa') return 'Activa';
    if (estado === 2 || String(estado).toLowerCase() === 'suspendida') return 'Suspendida';
    if (estado === 3 || String(estado).toLowerCase() === 'cancelada') return 'Cancelada';
    return String(estado);
  }

  mensajeModulo(modulo: ModuloSaaSUx): string {
    if (modulo.habilitado) return `${modulo.nombre} está incluido en el plan actual.`;
    if (String(modulo.motivo).toUpperCase() === 'NO_DISPONIBLE') {
      return `${modulo.nombre} no pudo validarse y permanece deshabilitado de forma segura.`;
    }
    return `${modulo.nombre} no está incluido en el plan actual.`;
  }

  private nuevaIdempotencyKey(): string {
    const uuid = globalThis.crypto?.randomUUID?.();
    return uuid ? `saas-ui-${uuid}` : `saas-ui-${Date.now()}-${Math.random().toString(16).slice(2)}`;
  }

  private ahoraLocal(): string {
    const now = new Date();
    const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
    return local.toISOString().slice(0, 16);
  }

  private mensajeError(err: HttpErrorResponse, fallback: string): string {
    return err.error?.detail ?? err.error?.message ?? fallback;
  }

  private noWhitespaceValidator(control: { value: unknown }): null | { whitespace: true } {
    return typeof control.value === 'string' && control.value.length > 0 && control.value.trim().length === 0
      ? { whitespace: true }
      : null;
  }
}
