
import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import {
  ConfigEmpresaTenant,
  EmpresaConfiguracionService,
  PlantillaCorreoEmpresa
} from '../../services/empresa-configuracion.service';

@Component({
  selector: 'app-empresa-configuracion-plantillas-tenant-card',
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
    <section class="card templates-card" aria-labelledby="tenant-mail-templates-title">
      <div class="header-row">
        <div>
          <h2 id="tenant-mail-templates-title">Plantillas de correo del tenant</h2>
          <p class="hint">Las plantillas se leen y modifican únicamente para la empresa validada por el servidor.</p>
        </div>
        <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading() || saving()">
          <mat-icon>refresh</mat-icon> Recargar
        </button>
      </div>

      @if (!empresaId()) {
        <p class="state error" role="alert">Selecciona una empresa autorizada antes de administrar sus plantillas.</p>
      } @else if (loading()) {
        <div class="state" role="status" aria-live="polite"><mat-spinner diameter="30"></mat-spinner><span>Cargando plantillas…</span></div>
      } @else {
        @if (error()) {
          <div class="error-banner" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error() }}</span></div>
        }

        <div class="layout">
          <div class="template-list" aria-label="Plantillas configuradas">
            <div class="list-header">
              <h3>Configuradas</h3>
              @if (puedeEditar()) {
                <button mat-button type="button" (click)="nueva()" [disabled]="saving()"><mat-icon>add</mat-icon> Nueva</button>
              }
            </div>
            @if (plantillas().length === 0) {
              <p class="hint">No hay plantillas configuradas.</p>
            } @else {
              @for (plantilla of plantillas(); track plantilla.tipoPlantilla) {
                <button
                  class="template-item"
                  type="button"
                  [class.selected]="tipoEditando() === plantilla.tipoPlantilla"
                  (click)="editar(plantilla)"
                  [attr.aria-pressed]="tipoEditando() === plantilla.tipoPlantilla">
                  <span><strong>{{ plantilla.tipoPlantilla }}</strong><small>{{ plantilla.asunto }}</small></span>
                  <span class="status" [class.inactive]="!plantilla.activa">{{ plantilla.activa ? 'Activa' : 'Inactiva' }}</span>
                </button>
              }
            }
          </div>

          <form [formGroup]="form" (ngSubmit)="guardar()" class="editor" aria-label="Editor de plantilla de correo">
            <h3>{{ tipoEditando() ? 'Editar plantilla' : 'Nueva plantilla' }}</h3>
            <mat-form-field appearance="outline">
              <mat-label>Tipo de plantilla</mat-label>
              <input matInput formControlName="tipoPlantilla" maxlength="80" [readonly]="!!tipoEditando()" autocomplete="off">
              <mat-hint>Identificador estable, por ejemplo FACTURA o COTIZACION.</mat-hint>
              <mat-error>El tipo es obligatorio.</mat-error>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Asunto</mat-label>
              <input matInput formControlName="asunto" maxlength="200">
              <mat-error>El asunto es obligatorio.</mat-error>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Cuerpo</mat-label>
              <textarea matInput formControlName="cuerpo" rows="8" maxlength="10000"></textarea>
              <mat-error>El cuerpo es obligatorio.</mat-error>
            </mat-form-field>
            <label class="check-row"><input type="checkbox" formControlName="activa"> Plantilla activa</label>

            @if (conflict()) {
              <div class="conflict" role="alert">
                <mat-icon>sync_problem</mat-icon>
                <span>La configuración cambió en otra sesión. Recarga antes de guardar nuevamente.</span>
              </div>
            }

            @if (!puedeEditar()) {
              <p class="hint readonly"><mat-icon>lock</mat-icon> Vista de solo lectura: falta Configuración: Editar.</p>
            } @else {
              <div class="actions">
                @if (tipoEditando()) {
                  <button mat-button type="button" (click)="desactivar()" [disabled]="saving() || conflict()">
                    <mat-icon>block</mat-icon> Desactivar
                  </button>
                }
                <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving() || conflict()">
                  @if (saving()) { <mat-spinner diameter="18"></mat-spinner> } @else { <mat-icon>save</mat-icon> }
                  Guardar plantilla
                </button>
              </div>
            }
          </form>
        </div>
      }
    </section>
  `,
  styles: [`
    .templates-card{margin-top:1.25rem;padding:1.25rem}.header-row,.list-header,.actions,.error-banner,.conflict,.readonly{display:flex;align-items:center;gap:.75rem}.header-row,.list-header{justify-content:space-between;flex-wrap:wrap}.hint{margin:.25rem 0;opacity:.78}.state{min-height:100px;display:flex;align-items:center;justify-content:center;gap:.75rem;text-align:center}.error,.error-banner,.conflict{color:var(--color-error,#b3261e)}.error-banner,.conflict,.readonly{padding:.75rem 1rem;border:1px solid rgba(0,0,0,.12);border-radius:8px}.layout{display:grid;grid-template-columns:minmax(230px,.75fr) minmax(0,1.25fr);gap:1rem;margin-top:1rem}.template-list,.editor{display:grid;align-content:start;gap:.75rem}.template-item{width:100%;display:flex;align-items:center;justify-content:space-between;gap:.75rem;text-align:left;padding:.8rem;border:1px solid rgba(0,0,0,.12);border-radius:8px;background:transparent;color:inherit;cursor:pointer}.template-item.selected{outline:2px solid var(--color-primary,#2563eb);outline-offset:1px}.template-item span:first-child{display:grid;gap:.2rem;min-width:0}.template-item small{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.status{font-size:.8rem;font-weight:700;color:#1b5e20}.status.inactive{color:#6b7280}.editor{padding:1rem;border:1px solid rgba(0,0,0,.12);border-radius:10px}.check-row{display:flex;align-items:center;gap:.5rem;min-height:44px}.actions{justify-content:flex-end;flex-wrap:wrap}.readonly{width:fit-content}@media(max-width:820px){.layout{grid-template-columns:1fr}.actions button{flex:1}}
  `]
})
export class EmpresaConfiguracionPlantillasTenantCardComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly empresaService = inject(EmpresaConfiguracionService);
  private readonly snackBar = inject(MatSnackBar);

  readonly empresaId = this.tenant.empresaIdVerificada;
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly conflict = signal(false);
  readonly puedeEditar = signal(false);
  readonly plantillas = signal<PlantillaCorreoEmpresa[]>([]);
  readonly tipoEditando = signal<string | null>(null);

  private version = 0;
  private initialized = false;
  private loadedEmpresaId: number | null = null;
  private loadGeneration = 0;

  private readonly tenantWatcher = effect(() => {
    const empresaId = this.empresaId();
    if (this.initialized && empresaId !== this.loadedEmpresaId) this.cargar();
  });

  readonly form = this.fb.group({
    tipoPlantilla: ['', [Validators.required, Validators.maxLength(80)]],
    asunto: ['', [Validators.required, Validators.maxLength(200)]],
    cuerpo: ['', [Validators.required, Validators.maxLength(10000)]],
    activa: [true]
  });

  ngOnInit(): void {
    this.puedeEditar.set(this.permisos.puede('Configuracion', 'Editar'));
    if (!this.puedeEditar()) this.form.disable();
    this.initialized = true;
    this.cargar();
  }

  cargar(): void {
    const empresaId = this.empresaId();
    const generation = ++this.loadGeneration;
    this.loadedEmpresaId = empresaId ?? null;
    this.error.set(null);
    this.conflict.set(false);

    if (!empresaId) {
      this.loading.set(false);
      this.plantillas.set([]);
      this.nueva();
      return;
    }

    this.loading.set(true);
    this.empresaService.getTenant(empresaId).pipe(finalize(() => {
      if (generation === this.loadGeneration) this.loading.set(false);
    })).subscribe({
      next: res => {
        if (generation !== this.loadGeneration || empresaId !== this.empresaId()) return;
        this.aplicar(res.data, empresaId);
      },
      error: err => {
        if (generation !== this.loadGeneration || empresaId !== this.empresaId()) return;
        this.error.set(err.error?.message ?? 'No se pudieron cargar las plantillas del tenant.');
      }
    });
  }

  nueva(): void {
    this.tipoEditando.set(null);
    this.form.reset({ tipoPlantilla: '', asunto: '', cuerpo: '', activa: true });
    this.conflict.set(false);
    if (!this.puedeEditar()) this.form.disable();
  }

  editar(plantilla: PlantillaCorreoEmpresa): void {
    this.tipoEditando.set(plantilla.tipoPlantilla);
    this.form.reset({
      tipoPlantilla: plantilla.tipoPlantilla,
      asunto: plantilla.asunto,
      cuerpo: plantilla.cuerpo,
      activa: plantilla.activa
    });
    this.conflict.set(false);
    if (!this.puedeEditar()) this.form.disable();
  }

  guardar(): void {
    const empresaId = this.empresaId();
    if (!empresaId || !this.puedeEditar() || this.form.invalid || this.saving() || this.conflict()) return;

    const value = this.form.getRawValue();
    const tipoPlantilla = value.tipoPlantilla!.trim();
    if (!tipoPlantilla) return;

    this.saving.set(true);
    this.error.set(null);
    this.empresaService.upsertTenantPlantilla(empresaId, tipoPlantilla, {
      asunto: value.asunto!.trim(),
      cuerpo: value.cuerpo!.trim(),
      activa: value.activa !== false,
      version: this.version
    }).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: res => {
        if (empresaId !== this.empresaId()) return;
        this.aplicar(res.data, empresaId);
        const actualizada = res.data.plantillasCorreo.find(p => p.tipoPlantilla === tipoPlantilla);
        if (actualizada) this.editar(actualizada);
        this.snackBar.open('Plantilla de correo guardada.', 'Cerrar', { duration: 4000 });
      },
      error: err => {
        if (empresaId !== this.empresaId()) return;
        this.manejarErrorMutacion(err, 'No se pudo guardar la plantilla de correo.');
      }
    });
  }

  desactivar(): void {
    const empresaId = this.empresaId();
    const tipoPlantilla = this.tipoEditando();
    if (!empresaId || !tipoPlantilla || !this.puedeEditar() || this.saving() || this.conflict()) return;

    this.saving.set(true);
    this.error.set(null);
    this.empresaService.desactivarTenantPlantilla(empresaId, tipoPlantilla, this.version)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: res => {
          if (empresaId !== this.empresaId()) return;
          this.aplicar(res.data, empresaId);
          const actualizada = res.data.plantillasCorreo.find(p => p.tipoPlantilla === tipoPlantilla);
          if (actualizada) this.editar(actualizada); else this.nueva();
          this.snackBar.open('Plantilla de correo desactivada.', 'Cerrar', { duration: 4000 });
        },
        error: err => {
          if (empresaId !== this.empresaId()) return;
          this.manejarErrorMutacion(err, 'No se pudo desactivar la plantilla de correo.');
        }
      });
  }

  private aplicar(config: ConfigEmpresaTenant, expectedEmpresaId: number): void {
    if (config.empresaId !== expectedEmpresaId || expectedEmpresaId !== this.empresaId()) {
      if (expectedEmpresaId === this.empresaId()) {
        this.error.set('El servidor devolvió plantillas de un tenant distinto al contexto verificado.');
      }
      return;
    }

    this.loadedEmpresaId = expectedEmpresaId;
    this.version = config.version;
    this.plantillas.set(config.plantillasCorreo ?? []);
    this.error.set(null);
    this.conflict.set(false);
  }

  private manejarErrorMutacion(err: any, fallback: string): void {
    if (err.status === 409 || err.status === 412) {
      this.conflict.set(true);
      return;
    }
    this.error.set(err.error?.message ?? fallback);
  }
}
