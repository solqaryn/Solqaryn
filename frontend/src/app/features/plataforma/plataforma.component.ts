import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { DirectorioPlataforma, PlataformaEmpresasService } from '../../core/auth/plataforma-empresas.service';

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
              <p>Consulta del directorio global, incluida la identidad de empresas inactivas, sin acceso a registros comerciales.</p>
              <button mat-stroked-button type="button" (click)="cargarEmpresas()" [disabled]="cargandoEmpresas()">Actualizar directorio</button>
              @if (directorio(); as listado) {
                @if (listado.puedeCrear) {
                  <p class="available">Alta autorizable por el backend; la operación requiere flujo de aprobación.</p>
                } @else {
                  <p class="pending">Alta desactivada por los controles de seguridad del servidor</p>
                }
              } @else {
                <p class="pending">La creación de empresas permanece bloqueada por defecto</p>
              }
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
        <mat-card class="directory-card" aria-label="Directorio de empresas">
          <mat-card-header>
            <mat-card-title>Directorio global de empresas</mat-card-title>
            <mat-card-subtitle>Solo metadatos de administración, sin información comercial de tenants</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            @if (cargandoEmpresas()) {
              <div class="directory-loading"><mat-spinner diameter="24" aria-label="Cargando directorio"></mat-spinner> Consultando empresas…</div>
            } @else if (errorEmpresas()) {
              <p role="alert" class="directory-error">{{ errorEmpresas() }}</p>
              <button mat-stroked-button type="button" (click)="cargarEmpresas()">Reintentar</button>
            } @else if (directorio(); as resultado) {
              <p class="directory-summary">Total de empresas: {{ resultado.total }}</p>
              @if (!resultado.items.length) {
                <p>No hay empresas para mostrar.</p>
              } @else {
                <div class="directory-scroll">
                  <table aria-label="Empresas registradas">
                    <thead><tr><th scope="col">Empresa</th><th scope="col">Estado</th><th scope="col">Usuarios activos</th></tr></thead>
                    <tbody>
                      @for (empresa of resultado.items; track empresa.id) {
                        <tr>
                          <td>{{ empresa.nombre }}</td>
                          <td>{{ empresa.activa ? 'Activa' : 'Inactiva' }}</td>
                          <td>{{ empresa.usuariosActivos }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              }
              @if (resultado.total > resultado.tamano) {
                <div class="pagination">
                  <button mat-stroked-button type="button" [disabled]="resultado.pagina <= 1" (click)="cargarEmpresas(resultado.pagina - 1)">Anterior</button>
                  <span>Página {{ resultado.pagina }} · {{ resultado.total }} empresas</span>
                  <button mat-stroked-button type="button" [disabled]="resultado.pagina * resultado.tamano >= resultado.total" (click)="cargarEmpresas(resultado.pagina + 1)">Siguiente</button>
                </div>
              }
            }
          </mat-card-content>
        </mat-card>
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
    .directory-card { margin-top: 20px; }
    .directory-card mat-card-content { padding: 20px 24px; }
    .directory-scroll { max-width: 100%; overflow-x: auto; }
    .directory-scroll table { width: 100%; border-collapse: collapse; }
    .directory-scroll th, .directory-scroll td { padding: 13px 12px; border-bottom: 1px solid #dfe5ec; text-align: left; }
    .directory-scroll th { font-size: .8rem; color: var(--color-text-muted); }
    .directory-loading { display: flex; align-items: center; gap: 12px; min-height: 52px; }
    .directory-error { color: var(--color-danger, #b3261e); }
    .directory-summary { color: var(--color-text-muted); }
    .pagination { display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 12px; margin-top: 16px; }
    .denied { max-width: 620px; }
    .denied mat-card-content { padding: 24px; }
    .denied mat-icon { color: var(--color-danger, #b3261e); }
    @media (max-width: 680px) { .claims, .modules { grid-template-columns: 1fr; } .heading__actions { width: 100%; } }
    @media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto; } }
  `]
})
export class PlataformaComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly empresasApi = inject(PlataformaEmpresasService);
  readonly sesion = this.plataforma.sesion;
  readonly validando = signal(false);
  readonly error = signal<string | null>(null);
  readonly directorio = signal<DirectorioPlataforma | null>(null);
  readonly cargandoEmpresas = signal(false);
  readonly errorEmpresas = signal<string | null>(null);

  constructor() {
    this.cargarEmpresas();
  }

  cargarEmpresas(pagina = 1): void {
    if (this.cargandoEmpresas()) return;
    this.cargandoEmpresas.set(true);
    this.errorEmpresas.set(null);
    this.directorio.set(null);
    this.empresasApi.listar(pagina).pipe(finalize(() => this.cargandoEmpresas.set(false)))
      .subscribe({
        next: respuesta => this.directorio.set(respuesta),
        error: () => this.errorEmpresas.set('No se pudo consultar el directorio global. No se mostrará información de empresas sin autorización verificada.')
      });
  }

  revalidar(): void {
    if (this.validando()) return;
    this.validando.set(true);
    this.error.set(null);
    this.plataforma.obtener().pipe(finalize(() => this.validando.set(false))).subscribe({
      error: () => this.error.set('La sesión global dejó de estar autorizada o no pudo verificarse. No se permite ninguna operación.')
    });
  }
}
