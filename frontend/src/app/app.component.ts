import { CommonModule, DOCUMENT } from '@angular/common';
import { Component, HostListener, Inject, OnDestroy } from '@angular/core';
import { RouterOutlet, RouterLink, Router, NavigationEnd } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
import { PermisosRuntimeService } from './core/auth/permisos-runtime.service';
import { ThemeApplierService } from './services/theme-applier.service';
import { EmpresaIdentidadService } from './services/empresa-identidad.service';
import { SessionActivityService } from './core/auth/session-activity.service';
import { TenantContextService } from './core/auth/tenant-context.service';
import { AppNavigationMenuComponent } from './shared/navigation/app-navigation-menu.component';
import { VaristorehnSeoService } from './features/varistorehn/varistorehn-seo.service';
import { VaristorehnIdentidadService } from './features/varistorehn/varistorehn-identidad.service';
import { PerformanceBaselineService } from './core/performance/performance-baseline.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, AppNavigationMenuComponent],
  template: `
    @if (auth.isAuthenticated()) {
      <a class="skip-link" href="#main-content">Saltar al contenido principal</a>
      <div class="sr-only" role="status" aria-live="polite" aria-atomic="true">{{ routeAnnouncement }}</div>
      <div class="layout">
        @if (sidebarAbierto) {
          <button class="overlay" type="button" (click)="cerrarSidebar(true)" aria-label="Cerrar menú lateral"></button>
        }
        <aside id="main-sidebar" class="sidebar" [class.abierto]="sidebarAbierto" aria-label="Menú principal">
          <div class="brand">
            <img class="brand-logo" [src]="identidad.logoUrl()" [alt]="identidad.nombreSistema()">
            <span>{{ identidad.nombreSistema() }}</span>
            <button type="button" class="cerrar-sidebar shell-icon-button" (click)="cerrarSidebar(true)" aria-label="Cerrar menú">
              <span class="material-icons" aria-hidden="true">close</span>
            </button>
          </div>
          <nav aria-label="Navegación principal" (click)="cerrarSidebarEnMovil()">
            <app-navigation-menu />
          </nav>
        </aside>
        <div class="main">
          <header class="topbar">
            <button
              id="menu-toggle"
              type="button"
              class="menu-toggle shell-icon-button"
              (click)="toggleSidebar()"
              aria-controls="main-sidebar"
              [attr.aria-expanded]="sidebarAbierto"
              [attr.aria-label]="sidebarAbierto ? 'Cerrar menú principal' : 'Abrir menú principal'">
              <span class="material-icons" aria-hidden="true">{{ sidebarAbierto ? 'close' : 'menu' }}</span>
            </button>
            <span class="header-text">
              @if (identidad.config().encabezadoActivo) {
                {{ identidad.descripcionSistema() }}
              }
            </span>
            <div class="user">
              <div class="user-copy">
                <span class="user-name">{{ auth.nombreCompleto() }}</span>
                <span class="user-role">{{ auth.rol() }}</span>
              </div>
              <button type="button" class="profile-button shell-icon-button" routerLink="/perfil" aria-label="Abrir mi perfil" title="Mi perfil">
                @if (auth.fotoPerfilUrl(); as foto) {
                  <img class="user-avatar" [src]="foto" [alt]="'Perfil de ' + (auth.nombreCompleto() || auth.nombreUsuario() || 'usuario')">
                } @else {
                  <span class="user-initials" aria-hidden="true">{{ inicialesUsuario() }}</span>
                }
              </button>
              <button type="button" class="topbar-icon-button shell-icon-button" (click)="logout()" aria-label="Cerrar sesión" title="Cerrar sesión">
                <span class="material-icons" aria-hidden="true">logout</span>
              </button>
            </div>
          </header>
          <main id="main-content" class="content" tabindex="-1">
            <router-outlet></router-outlet>
          </main>
          @if (identidad.config().piePaginaActivo || identidad.mostrarCopyright()) {
            <footer class="app-footer">
              @if (identidad.config().piePaginaActivo && identidad.config().piePaginaTexto) {
                <span>{{ identidad.config().piePaginaTexto }}</span>
              }
              @if (identidad.mostrarCopyright()) {
                <span>{{ identidad.copyright() }}</span>
              }
            </footer>
          }
        </div>
      </div>
    } @else {
      <router-outlet></router-outlet>
    }
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnDestroy {
  sidebarAbierto = false;
  routeAnnouncement = '';

  constructor(
    public auth: AuthService,
    public permisosRuntime: PermisosRuntimeService,
    public identidad: EmpresaIdentidadService,
    private sessionActivity: SessionActivityService,
    private tenantContext: TenantContextService,
    private router: Router,
    private themeApplier: ThemeApplierService,
    private seo: VaristorehnSeoService,
    private tiendaIdentidad: VaristorehnIdentidadService,
    private performanceBaseline: PerformanceBaselineService,
    @Inject(DOCUMENT) private document: Document
  ) {
    this.performanceBaseline.start();
    this.aplicarContextoRuta(this.router.url);
    if (this.auth.isAuthenticated()) {
      this.permisosRuntime.cargar().subscribe();
      this.sessionActivity.iniciar();
    }
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.aplicarContextoRuta(event.urlAfterRedirects);
        this.cerrarSidebar();
        this.gestionarFocoTrasNavegacion();
      }
    });
  }

  ngOnDestroy(): void {
    this.sessionActivity.detener();
    this.document.body.style.removeProperty('overflow');
  }

  toggleSidebar(): void {
    if (this.sidebarAbierto) {
      this.cerrarSidebar(true);
      return;
    }

    this.sidebarAbierto = true;
    this.sincronizarScrollMovil();
    if (window.innerWidth <= 900) {
      window.setTimeout(() => {
        this.document.querySelector<HTMLElement>('#main-sidebar .cerrar-sidebar')?.focus();
      });
    }
  }

  cerrarSidebar(devolverFoco = false): void {
    const estabaAbierto = this.sidebarAbierto;
    this.sidebarAbierto = false;
    this.sincronizarScrollMovil();
    if (devolverFoco && estabaAbierto && window.innerWidth <= 900) {
      window.setTimeout(() => this.document.getElementById('menu-toggle')?.focus());
    }
  }

  cerrarSidebarEnMovil(): void {
    if (window.innerWidth <= 900) this.cerrarSidebar();
  }

  inicialesUsuario(): string {
    const nombre = this.auth.nombreCompleto()?.trim() || this.auth.nombreUsuario()?.trim() || 'Usuario';
    return nombre.split(/\s+/).slice(0, 2).map(parte => parte.charAt(0).toUpperCase()).join('');
  }

  @HostListener('window:keydown.escape')
  onEscape(): void {
    if (this.sidebarAbierto) this.cerrarSidebar(true);
  }

  @HostListener('window:resize')
  onResize(): void {
    if (window.innerWidth > 900 && this.sidebarAbierto) this.cerrarSidebar();
  }

  logout(): void {
    this.cerrarSidebar();
    this.sessionActivity.cerrarManual();
  }

  private aplicarContextoRuta(url: string): void {
    if (this.esRutaTienda(url)) {
      this.tiendaIdentidad.cargar().subscribe((bootstrap) => {
        if (bootstrap) this.themeApplier.aplicar(bootstrap.tema);
        else this.themeApplier.aplicarTemaGuardado();
        this.seo.aplicarRuta(url, this.tiendaIdentidad.config().nombreComercial || 'Tienda');
      });
      return;
    }

    if (this.auth.isAuthenticated() && this.tenantContext.tieneContextoVerificado()) {
      this.themeApplier.aplicarTemaGuardado();
      this.identidad.cargar().subscribe();
    } else {
      this.themeApplier.aplicarTemaPlataforma();
      this.identidad.usarPlataforma();
    }

    this.seo.aplicarNoIndex('SOLQARYN');
  }

  private esRutaTienda(url: string): boolean {
    const path = (url || '/').split(/[?#]/, 1)[0].replace(/\/+$/, '') || '/';
    return path === '/varistorehn' || path.startsWith('/varistorehn/');
  }

  private gestionarFocoTrasNavegacion(): void {
    if (!this.auth.isAuthenticated()) return;

    window.setTimeout(() => {
      const main = this.document.getElementById('main-content');
      if (!main) return;

      const titulo = main.querySelector('h1')?.textContent?.trim();
      this.routeAnnouncement = titulo ? `Página cargada: ${titulo}` : 'Página cargada';
      main.focus({ preventScroll: true });
      main.scrollIntoView({ block: 'start', behavior: 'auto' });
    });
  }

  private sincronizarScrollMovil(): void {
    if (this.sidebarAbierto && window.innerWidth <= 900) {
      this.document.body.style.overflow = 'hidden';
    } else {
      this.document.body.style.removeProperty('overflow');
    }
  }
}
