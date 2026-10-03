
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
import { ConfigEmpresaTenant, EmpresaConfiguracionService } from '../../services/empresa-configuracion.service';

@Component({
  selector: 'app-empresa-configuracion-tenant-card',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule],
  template: `
    <section class="card tenant-card" aria-labelledby="tenant-config-title">
      <div class="header-row">
        <div>
          <h2 id="tenant-config-title">Configuración del tenant activo</h2>
          <p class="hint">La empresa efectiva proviene del contexto validado por backend. La UI no puede cambiar el tenant de esta operación.</p>
        </div>
        <button mat-stroked-button type="button" (click)="cargar()" [disabled]="loading() || saving()">
          <mat-icon>refresh</mat-icon> Recargar
        </button>
      </div>

      @if (!empresaId()) {
        <div class="state error" role="alert">
          <mat-icon>domain_disabled</mat-icon>
          <span>No existe un contexto tenant verificado. Selecciona una empresa autorizada antes de administrar su configuración.</span>
        </div>
      } @else if (loading()) {
        <div class="state" role="status" aria-live="polite"><mat-spinner diameter="32"></mat-spinner><span>Cargando configuración tenant…</span></div>
      } @else if (error()) {
        <div class="state error" role="alert">
          <mat-icon>error_outline</mat-icon><span>{{ error() }}</span>
          <button mat-button type="button" (click)="cargar()">Reintentar</button>
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="guardar()" class="tenant-form" aria-label="Configuración del tenant activo">
          <div class="section-title"><h3>Datos de empresa</h3><span>Empresa #{{ empresaId() }}</span></div>
          <div class="grid">
            <mat-form-field appearance="outline"><mat-label>Nombre</mat-label><input matInput formControlName="nombre" maxlength="200" autocomplete="organization"><mat-error>El nombre es obligatorio.</mat-error></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>RTN</mat-label><input matInput formControlName="rtn" maxlength="40"></mat-form-field>
            <mat-form-field appearance="outline" class="wide"><mat-label>Dirección</mat-label><textarea matInput formControlName="direccion" rows="2" maxlength="500"></textarea></mat-form-field>
          </div>

          <h3>Logo</h3>
          <div class="logo-row">
            @if (logoUrl()) {
              <img class="tenant-logo" [src]="logoUrl()" alt="Logo actual del tenant">
            } @else {
              <div class="logo-placeholder" aria-label="Tenant sin logo"><mat-icon>image_not_supported</mat-icon></div>
            }
            <div class="logo-actions">
              <input #logoInput type="file" accept="image/*" hidden (change)="subirLogo($event)">
              <button mat-stroked-button type="button" (click)="logoInput.click()" [disabled]="!puedeEditar() || saving()">
                <mat-icon>upload</mat-icon> Subir logo
              </button>
              @if (logoUrl()) {
                <button mat-button type="button" (click)="eliminarLogo()" [disabled]="!puedeEditar() || saving()">
                  <mat-icon>delete_outline</mat-icon> Quitar logo
                </button>
              }
            </div>
          </div>

          <h3>Localización y moneda</h3>
          <div class="grid">
            <mat-form-field appearance="outline"><mat-label>Moneda ISO</mat-label><input matInput formControlName="moneda" maxlength="3"><mat-error>Usa un código ISO de 3 letras.</mat-error></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Zona horaria IANA</mat-label><input matInput formControlName="zonaHoraria" maxlength="100"><mat-error>La zona horaria es obligatoria.</mat-error></mat-form-field>
          </div>

          <h3>Impuestos y emisión</h3>
          <p class="hint">Los contratos avanzados se mantienen como JSON validado por backend hasta que N6.7 tenga editores especializados.</p>
          <div class="grid">
            <mat-form-field appearance="outline" class="wide"><mat-label>Política de impuestos (JSON)</mat-label><textarea matInput formControlName="impuestosJson" rows="5" spellcheck="false"></textarea></mat-form-field>
            <mat-form-field appearance="outline" class="wide"><mat-label>Parámetros de emisión (JSON)</mat-label><textarea matInput formControlName="emisionJson" rows="5" spellcheck="false"></textarea></mat-form-field>
          </div>

          <h3>Correo</h3>
          <div class="mail-status" [class.configured]="correoConfigurado()">
            <mat-icon>{{ correoConfigurado() ? 'verified' : 'info' }}</mat-icon>
            <span>{{ correoConfigurado() ? 'Credencial de correo configurada. El secreto no se muestra.' : 'No hay credencial de correo activa.' }}</span>
          </div>
          <div class="grid">
            <mat-form-field appearance="outline"><mat-label>Correo remitente</mat-label><input matInput type="email" formControlName="correoRemitente" autocomplete="email"><mat-error>Correo no válido.</mat-error></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Nombre del remitente</mat-label><input matInput formControlName="correoNombreRemitente" maxlength="200"></mat-form-field>
          </div>

          <h3>Plantillas</h3>
          @if (plantillas().length === 0) {
            <p class="hint">No hay plantillas de correo configuradas para esta empresa.</p>
          } @else {
            <div class="templates" role="list">
              @for (plantilla of plantillas(); track plantilla.tipoPlantilla) {
                <article role="listitem"><strong>{{ plantilla.tipoPlantilla }}</strong><span>{{ plantilla.activa ? 'Activa' : 'Inactiva' }}</span><small>{{ plantilla.asunto }}</small></article>
              }
            </div>
          }

          @if (conflict()) {
            <div class="conflict" role="alert">
              <mat-icon>sync_problem</mat-icon>
              <span>La configuración cambió en otra sesión. Recarga antes de volver a guardar para evitar sobrescritura silenciosa.</span>
              <button mat-stroked-button type="button" (click)="cargar()">Recargar versión</button>
            </div>
          }

          @if (!puedeEditar()) {
            <p class="hint readonly"><mat-icon>lock</mat-icon> Vista de solo lectura: falta el permiso Configuración: Editar.</p>
          } @else {
            <div class="actions">
              <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving() || conflict()">
                @if (saving()) { <mat-spinner diameter="18"></mat-spinner> } @else { <mat-icon>save</mat-icon> }
                Guardar configuración tenant
              </button>
            </div>
          }
        </form>
      }
    </section>
  `,
  styles: [`
    .tenant-card{margin-top:1.25rem;padding:1.25rem}.header-row,.section-title,.actions,.mail-status,.conflict,.readonly,.logo-row,.logo-actions{display:flex;align-items:center;gap:.75rem}.header-row,.section-title{justify-content:space-between;flex-wrap:wrap}h2,h3{margin:0}.tenant-form{display:grid;gap:1rem}.hint{margin:.25rem 0;opacity:.78}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.wide{grid-column:1/-1}.state{min-height:120px;display:flex;align-items:center;justify-content:center;gap:.75rem;text-align:center}.error,.conflict{color:var(--color-error,#b3261e)}.state.error{flex-wrap:wrap}.mail-status,.conflict,.readonly{padding:.75rem 1rem;border:1px solid rgba(0,0,0,.12);border-radius:8px}.mail-status.configured{color:#1b5e20}.logo-row{align-items:flex-start;flex-wrap:wrap}.tenant-logo,.logo-placeholder{width:120px;height:72px;border:1px solid rgba(0,0,0,.12);border-radius:8px;object-fit:contain;background:rgba(0,0,0,.02)}.logo-placeholder{display:grid;place-items:center}.logo-actions{flex-wrap:wrap}.templates{display:grid;gap:.5rem}.templates article{display:grid;grid-template-columns:minmax(140px,1fr) auto;gap:.25rem 1rem;padding:.75rem 1rem;border:1px solid rgba(0,0,0,.12);border-radius:8px}.templates small{grid-column:1/-1;opacity:.75}.actions{justify-content:flex-end}.readonly{width:fit-content}@media(max-width:760px){.grid{grid-template-columns:1fr}.wide{grid-column:auto}.header-row>button,.actions button,.conflict button{width:100%}.conflict{align-items:flex-start;flex-wrap:wrap}.logo-actions{width:100%}.logo-actions button{flex:1}}
  `]
})
export class EmpresaConfiguracionTenantCardComponent implements OnInit {
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
  readonly correoConfigurado = signal(false);
  readonly logoUrl = signal<string | null>(null);
  readonly plantillas = signal<ConfigEmpresaTenant['plantillasCorreo']>([]);
  private version = 0;
  private initialized = false;
  private loadedEmpresaId: number | null = null;
  private loadGeneration = 0;

  private readonly tenantWatcher = effect(() => {
    const empresaId = this.empresaId();
    if (this.initialized && empresaId !== this.loadedEmpresaId) this.cargar();
  });

  readonly form = this.fb.group({
    nombre: ['', [Validators.required, Validators.maxLength(200)]],
    rtn: ['', Validators.maxLength(40)],
    direccion: ['', Validators.maxLength(500)],
    moneda: ['HNL', [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)]],
    zonaHoraria: ['America/Tegucigalpa', [Validators.required, Validators.maxLength(100)]],
    impuestosJson: ['{}', Validators.required],
    emisionJson: ['{}', Validators.required],
    correoRemitente: ['', Validators.email],
    correoNombreRemitente: ['', Validators.maxLength(200)]
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
    if (!empresaId) {
      this.loading.set(false);
      this.error.set(null);
      this.conflict.set(false);
      this.logoUrl.set(null);
      this.plantillas.set([]);
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.conflict.set(false);
    this.empresaService.getTenant(empresaId).pipe(finalize(() => {
      if (generation === this.loadGeneration) this.loading.set(false);
    })).subscribe({
      next: res => {
        if (generation !== this.loadGeneration || empresaId !== this.empresaId()) return;
        this.aplicar(res.data, empresaId);
      },
      error: err => {
        if (generation !== this.loadGeneration || empresaId !== this.empresaId()) return;
        this.error.set(err.error?.message ?? 'No se pudo cargar la configuración del tenant activo.');
      }
    });
  }

  guardar(): void {
    const empresaId = this.empresaId();
    if (!empresaId || !this.puedeEditar() || this.form.invalid || this.conflict()) return;
    if (!this.jsonValido('impuestosJson') || !this.jsonValido('emisionJson')) return;

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.empresaService.updateTenant(empresaId, {
      nombre: value.nombre!.trim(),
      rtn: value.rtn?.trim() || null,
      direccion: value.direccion?.trim() || null,
      moneda: value.moneda!.trim().toUpperCase(),
      zonaHoraria: value.zonaHoraria!.trim(),
      impuestosJson: value.impuestosJson!.trim(),
      emisionJson: value.emisionJson!.trim(),
      correoRemitente: value.correoRemitente?.trim() || null,
      correoNombreRemitente: value.correoNombreRemitente?.trim() || null,
      version: this.version
    }).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: res => {
        if (empresaId !== this.empresaId()) return;
        this.aplicar(res.data, empresaId);
        this.snackBar.open('Configuración del tenant actualizada.', 'Cerrar', { duration: 4000 });
      },
      error: err => {
        if (empresaId !== this.empresaId()) return;
        this.manejarErrorMutacion(err, 'No se pudo guardar la configuración del tenant activo.');
      }
    });
  }

  subirLogo(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    const empresaId = this.empresaId();
    if (!file || !empresaId || !this.puedeEditar() || this.saving()) return;

    this.saving.set(true);
    this.error.set(null);
    this.empresaService.updateTenantLogo(empresaId, file).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: res => {
        if (empresaId !== this.empresaId()) return;
        this.aplicar(res.data, empresaId);
        this.snackBar.open('Logo del tenant actualizado.', 'Cerrar', { duration: 4000 });
      },
      error: err => {
        if (empresaId !== this.empresaId()) return;
        this.manejarErrorMutacion(err, 'No se pudo actualizar el logo del tenant.');
      }
    });
  }

  eliminarLogo(): void {
    const empresaId = this.empresaId();
    if (!empresaId || !this.puedeEditar() || this.saving() || !this.logoUrl()) return;

    this.saving.set(true);
    this.error.set(null);
    this.empresaService.restaurarTenantLogo(empresaId).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: res => {
        if (empresaId !== this.empresaId()) return;
        this.aplicar(res.data, empresaId);
        this.snackBar.open('Logo del tenant eliminado.', 'Cerrar', { duration: 4000 });
      },
      error: err => {
        if (empresaId !== this.empresaId()) return;
        this.manejarErrorMutacion(err, 'No se pudo eliminar el logo del tenant.');
      }
    });
  }

  private aplicar(config: ConfigEmpresaTenant, expectedEmpresaId = this.empresaId()): void {
    if (!expectedEmpresaId || expectedEmpresaId !== this.empresaId() || config.empresaId !== expectedEmpresaId) {
      if (expectedEmpresaId === this.empresaId()) {
        this.error.set('El servidor devolvió una configuración que no coincide con el tenant verificado.');
      }
      return;
    }
    this.loadedEmpresaId = expectedEmpresaId;
    this.version = config.version;
    this.correoConfigurado.set(config.correoConfigurado);
    this.logoUrl.set(config.logoUrl ?? null);
    this.plantillas.set(config.plantillasCorreo ?? []);
    this.form.reset({
      nombre: config.nombre,
      rtn: config.rtn ?? '',
      direccion: config.direccion ?? '',
      moneda: config.moneda,
      zonaHoraria: config.zonaHoraria,
      impuestosJson: config.impuestosJson || '{}',
      emisionJson: config.emisionJson || '{}',
      correoRemitente: config.correoRemitente ?? '',
      correoNombreRemitente: config.correoNombreRemitente ?? ''
    });
    if (!this.puedeEditar()) this.form.disable();
    this.conflict.set(false);
    this.error.set(null);
  }

  private manejarErrorMutacion(err: any, fallback: string): void {
    if (err.status === 409 || err.status === 412) {
      this.conflict.set(true);
      return;
    }
    this.error.set(err.error?.message ?? fallback);
  }

  private jsonValido(controlName: 'impuestosJson' | 'emisionJson'): boolean {
    const control = this.form.controls[controlName];
    try {
      JSON.parse(control.value || '{}');
      control.setErrors(null);
      return true;
    } catch {
      control.setErrors({ json: true });
      control.markAsTouched();
      this.error.set(controlName === 'impuestosJson' ? 'La política de impuestos debe ser JSON válido.' : 'Los parámetros de emisión deben ser JSON válido.');
      return false;
    }
  }
}
