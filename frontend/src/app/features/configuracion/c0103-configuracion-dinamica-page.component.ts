import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmpresaConfiguracionTenantCardComponent } from './empresa-configuracion-tenant-card.component';

// V24 C01-03.M04.01: reuses real tenant HTTP/DI and server-side permissions.
@Component({
  selector: 'app-c0103-configuracion-dinamica-page',
  standalone: true,
  imports: [RouterLink, EmpresaConfiguracionTenantCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="settings-surface" id="c0103-configuracion-principal">
      <header class="surface-header">
        <nav aria-label="Ruta de configuración">
          <a routerLink="/configuracion" class="back-link">Configuración general</a>
          <span aria-hidden="true">/</span>
          <span aria-current="page">Configuración dinámica</span>
        </nav>
        <div class="intro">
          <div>
            <p class="eyebrow">Empresa activa</p>
            <h1>Configuración dinámica</h1>
            <p class="lead" id="c0103-configuracion-descripcion">
              Consulta y edita los parámetros disponibles de la empresa verificada.
              Los permisos, la pertenencia y la versión se validan en el servidor.
            </p>
          </div>
          <a routerLink="/configuracion" class="secondary-link">Ver todos los ajustes</a>
        </div>
      </header>
      <section class="surface-content" aria-label="Parámetros empresariales"
        aria-describedby="c0103-configuracion-descripcion">
        <app-empresa-configuracion-tenant-card />
      </section>
      <p class="scope-note">
        Esta pantalla reutiliza el contrato empresarial vigente: no permite crear claves
        globales arbitrarias ni modificar credenciales o permisos desde la interfaz.
      </p>
    </main>
  `,
  styles: [`
    :host { display:block; min-width:0; width:100%; }
    .settings-surface { box-sizing:border-box; width:100%; min-width:0; max-width:1040px; margin-inline:auto; padding:clamp(12px,2.4vw,32px); overflow-wrap:anywhere; }
    .surface-header { margin-bottom:24px; }
    nav { display:flex; flex-wrap:wrap; align-items:center; gap:8px; font-size:.875rem; color:var(--text-secondary,#52606d); }
    a { color:var(--color-primary,#185abc); text-underline-offset:3px; }
    a:focus-visible { outline:3px solid currentColor; outline-offset:3px; border-radius:4px; }
    .intro { display:flex; align-items:flex-end; justify-content:space-between; flex-wrap:wrap; gap:16px; margin-top:22px; }
    h1 { margin:0; line-height:1.13; font-size:clamp(1.55rem,3.8vw,2.25rem); }
    .eyebrow { margin:0 0 6px; color:var(--text-secondary,#52606d); font-size:.77rem; font-weight:650; letter-spacing:.055em; text-transform:uppercase; }
    .lead { max-width:68ch; margin:12px 0 0; color:var(--text-secondary,#52606d); line-height:1.6; }
    .secondary-link { flex:0 0 auto; font-weight:600; }
    .surface-content { min-width:0; width:100%; }
    .scope-note { margin:18px 0 0; font-size:.825rem; line-height:1.55; color:var(--text-secondary,#52606d); }
    @media (max-width:390px) { .settings-surface { padding:12px; } .intro { align-items:flex-start; } }
  `]
})
export class C0103ConfiguracionDinamicaPageComponent {}
