import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

@Component({
  selector: 'app-plataforma',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="plataforma" aria-labelledby="plataforma-title">
      <header class="heading">
        <div class="heading__text">
          <span class="eyebrow"><mat-icon aria-hidden="true">verified_user</mat-icon> SOLQARYN · Plataforma</span>
          <h1 id="plataforma-title">Administración global</h1>
          <p>Acceso independiente de una empresa. La autorización se comprueba directamente en el servidor.</p>
        </div>
        <div class="heading__actions">
          <button mat-stroked-button type="button" (click)="revalidar()" [disabled]="validando()">
            @if (validando()) { <mat-spinner diameter="18" aria-label="Verificando autoridad"></mat-spinner> }
            @else { <mat-icon>refresh</mat-icon> }
            Revalidar sesión
          </button>
          <a mat-button routerLink="/login"><mat-icon>apartment</mat-icon> Acceder a una empresa</a>
        </div>
      </header>

      @if (sesion(); as actual) {
        <mat-card class="authority-card">
          <mat-card-header>
            <mat-icon mat-card-avatar aria-hidden="true">admin_panel_settings</mat-icon>
            <mat-card-title>Identidad de plataforma verificada</mat-card-title>
            <mat-card-subtitle>Confirmada mediante JWT vigente y autoridad MySQL, no mediante permisos empresariales</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <dl class="claims">
              <div><dt>Cuenta</dt><dd>{{ actual.nombreUsuario }}</dd></div>
              <div><dt>Rol</dt><dd>{{ actual.rolNombre }}</dd></div>
              <div><dt>Ámbito</dt><dd>{{ actual.ambito }}</dd></div>
            </dl>
          </mat-card-content>
        </mat-card>
        <div class="modules" aria-label="Capacidades de la plataforma">
          <mat-card class="module-card">
            <mat-card-content>
              <mat-icon aria-hidden="true">domain</mat-icon>
              <h2>Gestión de empresas</h2>
              <p>El alta empresarial permanece cerrada hasta que las dos banderas de certificación y onboarding estén habilitadas de forma controlada en el servidor.</p>
              <span class="pending">Sin habilitación operativa</span>
            </mat-card-content>
          </mat-card>
          <mat-card class="module-card">
            <mat-card-content>
              <mat-icon aria-hidden="true">security</mat-icon>
              <h2>Seguridad y trazabilidad</h2>
              <p>Cada comprobación de acceso global es auditada por el backend. Los administradores empresariales no reciben privilegios de plataforma.</p>
              <span class="available">Control de sesión activo</span>
            </mat-card-content>
          </mat-card>
        </div>
      } @else {
        <mat-card class="denied">
          <mat-card-content>
            <mat-icon aria-hidden="true">gpp_bad</mat-icon>
            <h2>Sin autoridad global verificable</h2>
            <p role="alert">{{ error() || 'No se recibió una sesión de plataforma válida.' }}</p>
            <a mat-stroked-button routerLink="/login">Regresar al acceso</a>
          </mat-card-content>
        </mat-card>
      }
    </section>
  `,
  styles: [`
    :host { display: block; }
    .plataforma { max-width: 1120px; margin: 0 auto; padding: clamp(16px, 3vw, 36px); }
    .heading { display: flex; align-items: flex-end; justify-content: space-between; flex-wrap: wrap; gap: 20px; margin-bottom: 24px; }
    .heading__text { max-width: 680px; }
    .eyebrow { display: inline-flex; align-items: center; gap: 8px; font-size: .8rem; font-weight: 750; letter-spacing: .08em; text-transform: uppercase; color: var(--color-primary); }
    .eyebrow mat-icon { font-size: 20px; width: 20px; height: 20px; }
    h1 { margin: 10px 0; font-size: clamp(1.8rem, 3.5vw, 2.8rem); line-height: 1.15; }
    p { line-height: 1.6; }
    .heading p { margin: 0; color: var(--color-text-muted); }
    .heading__actions { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; }
    .heading__actions button, .heading__actions a { min-height: 44px; }
    .authority-card { margin-bottom: 20px; }
    .claims { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; margin: 24px 0 4px; }
    .claims div { border-radius: 12px; padding: 16px; background: var(--color-background, #f8fafc); min-width: 0; }
    dt { color: var(--color-text-muted); font-size: .85rem; }
    dd { margin: 6px 0 0; font-weight: 700; overflow-wrap: anywhere; }
    .modules { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 18px; }
    .module-card { height: 100%; }
    .module-card mat-card-content { padding: 24px; }
    .module-card mat-icon { color: var(--color-primary); font-size: 30px; width: 30px; height: 30px; }
    h2 { margin: 12px 0 8px; font-size: 1.15rem; }
    .module-card p { color: var(--color-text-muted); }
    .pending, .available { display: inline-block; border: 1px solid currentColor; border-radius: 999px; padding: 5px 11px; font-weight: 650; font-size: .8rem; }
    .pending { color: #955500; }
    .available { color: #22693d; }
    .denied { max-width: 620px; }
    .denied mat-card-content { padding: 24px; }
    .denied mat-icon { color: var(--color-danger, #b3261e); }
    @media (max-width: 680px) { .claims, .modules { grid-template-columns: 1fr; } .heading__actions { width: 100%; } }
    @media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto; } }
  `]
})
export class PlataformaComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly sesion = this.plataforma.sesion;
  readonly validando = signal(false);
  readonly error = signal<string | null>(null);

  revalidar(): void {
    if (this.validando()) return;
    this.validando.set(true);
    this.error.set(null);
    this.plataforma.obtener().pipe(finalize(() => this.validando.set(false))).subscribe({
      error: () => this.error.set('La sesión global dejó de estar autorizada o no pudo verificarse. No se permite ninguna operación.')
    });
  }
}
