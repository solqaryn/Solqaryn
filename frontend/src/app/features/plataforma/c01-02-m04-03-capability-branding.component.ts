import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TemaVisualService } from '../../services/tema-visual.service';
import { c0102CatalogoDeRutas } from './c01-02-m04-01-capability-catalog.component';

export function c0102ColorSeguro(value: unknown): string | null {
  return typeof value === 'string' && /^#(?:[a-f0-9]{3}|[a-f0-9]{6})$/i.test(value) ? value : null;
}
interface MarcaSegura { primario: string; secundario: string; acento: string; }

@Component({
  selector: 'app-c0102-capability-branding',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="marca-capacidades" aria-labelledby="marca-title">
      <a routerLink="/plataforma">Volver a plataforma</a>
      <h1 id="marca-title">Branding y capacidades permitidas</h1>
      <p role="note">Vista informativa. Colores y rutas del cliente nunca conceden permisos del servidor.</p>
      @switch (estado()) {
        @case ('DENEGADO') { <p role="alert" data-testid="marca-denegado">No se verificó la autoridad global.</p> }
        @case ('SIN_TENANT') { <p role="alert" data-testid="marca-sin-tenant">No hay empresa con membresía verificada.</p> }
        @case ('SIN_PERMISOS') { <p role="alert" data-testid="marca-sin-permisos">Configuración o ruta no autorizada.</p> }
        @case ('CARGANDO') { <p role="status" data-testid="marca-cargando">Consultando configuración…</p> }
        @case ('ERROR') { <p role="alert" data-testid="marca-error">No se confirmó la configuración segura.</p> }
        @case ('LISTO') {
          <p role="status" data-testid="marca-listo">Tema del contexto actual; cada operación exige autorización del servidor.</p>
          <section aria-label="Colores sanitizados" class="paleta">
            <div><span class="muestra" [style.background-color]="marca()?.primario"></span>Primario</div>
            <div><span class="muestra" [style.background-color]="marca()?.secundario"></span>Secundario</div>
            <div><span class="muestra" [style.background-color]="marca()?.acento"></span>Acento</div>
          </section>
          <h2>Componentes con señales de permiso y ruta</h2>
          <ul data-testid="marca-rutas">
            @for (ruta of rutasPermitidas(); track $index) {
              <li>{{ ruta.modulo }} · {{ ruta.accion }} — {{ ruta.ruta }}</li>
            }
          </ul>
        }
      }
    </main>
  `,
  styles: [`
    .marca-capacidades { max-width: 1080px; margin: auto; padding: clamp(16px, 3vw, 32px); overflow-wrap: anywhere; }
    .paleta { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 180px), 1fr)); gap: 12px; }
    .paleta div { display: flex; align-items: center; gap: 10px; padding: 12px; border: 1px solid #8193a5; border-radius: 8px; }
    .muestra { display: inline-block; width: 36px; height: 36px; flex: none; border: 1px solid #777; border-radius: 6px; }
    li { margin: 8px 0; } a:focus-visible { outline: 3px solid #0868ad; outline-offset: 4px; }
  `]
})
export class C0102CapabilityBrandingComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly sesion = inject(PlataformaSesionService);
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly temas = inject(TemaVisualService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly carga = signal<'pendiente' | 'lista' | 'error'>('pendiente');
  private readonly recibido = signal<MarcaSegura | null>(null);
  private readonly empresaOrigen = signal<number | null>(null);
  readonly autorizado = computed(() => this.sesion.sesion()?.ambito === 'Plataforma' && this.sesion.sesion()?.esAdministrador === true);
  private readonly permisoTema = computed(() =>
    this.autorizado() && this.tenant.tieneContextoVerificado() &&
    this.permisos.cargado() && this.permisos.puede('Configuracion', 'Ver') &&
    this.permisos.rutaHabilitada('/configuracion')
  );
  readonly marca = computed(() => this.permisoTema() &&
    this.empresaOrigen() === this.tenant.empresaIdVerificada() ? this.recibido() : null);
  readonly rutasPermitidas = computed(() => {
    if (!this.marca()) return [];
    return c0102CatalogoDeRutas(this.router.config).filter(r =>
      this.permisos.puede(r.modulo, r.accion) && this.permisos.rutaHabilitada(r.ruta));
  });
  readonly estado = computed(() => {
    if (!this.autorizado()) return 'DENEGADO';
    if (!this.tenant.tieneContextoVerificado()) return 'SIN_TENANT';
    if (this.carga() === 'error') return 'ERROR';
    if (!this.permisos.cargado()) return 'CARGANDO';
    if (!this.permisoTema()) return 'SIN_PERMISOS';
    return this.carga() === 'lista' && this.marca() ? 'LISTO' : 'CARGANDO';
  });

  ngOnInit(): void {
    if (!this.autorizado() || !this.tenant.tieneContextoVerificado()) return;
    const empresa = this.tenant.empresaIdVerificada();
    if (!empresa) return;
    if (this.permisos.cargado()) { this.leerTema(empresa); return; }
    this.permisos.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ok => {
        if (ok && this.contextoCoincide(empresa)) this.leerTema(empresa);
        else this.carga.set('error');
      },
      error: () => this.carga.set('error')
    });
  }
  private contextoCoincide(empresa: number): boolean {
    return this.autorizado() && this.tenant.tieneContextoVerificado() &&
      this.tenant.empresaIdVerificada() === empresa;
  }
  private leerTema(empresa: number): void {
    if (!this.contextoCoincide(empresa) || !this.permisoTema()) { this.carga.set('error'); return; }
    this.temas.get().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: respuesta => {
        if (!this.contextoCoincide(empresa) || !this.permisoTema()) return;
        const data = respuesta?.success === true ? respuesta.data : null;
        const primario = c0102ColorSeguro(data?.colorPrimario);
        const secundario = c0102ColorSeguro(data?.colorSecundario);
        const acento = c0102ColorSeguro(data?.colorAcento);
        if (!primario || !secundario || !acento) { this.recibido.set(null); this.carga.set('error'); return; }
        this.empresaOrigen.set(empresa);
        this.recibido.set({ primario, secundario, acento });
        this.carga.set('lista');
      },
      error: () => this.carga.set('error')
    });
  }
}
