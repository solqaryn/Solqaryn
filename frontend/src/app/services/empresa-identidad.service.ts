import { Injectable, computed, signal } from '@angular/core';
import { catchError, map, of, switchMap } from 'rxjs';
import { EmpresaConfiguracion } from '../core/models/empresa-configuracion.model';
import { EmpresaConfiguracionService } from './empresa-configuracion.service';

const DEFAULT_CONFIG: EmpresaConfiguracion = {
  id: 0,
  nombreComercial: 'SOLQARYN',
  eslogan: 'Plataforma empresarial',
  nombreVisibleSistema: 'SOLQARYN',
  descripcionSistema: 'Plataforma empresarial',
  mensajeLogin: 'Inicia sesión en SOLQARYN',
  copyright: '© 2026 SOLQARYN. Todos los derechos reservados.',
  mostrarCopyright: true,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: true,
  piePaginaActivo: true,
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy'
};

/**
 * C02-01.M04.03: presentation-only branding. Never treats a public theme,
 * URL or client preference as an authenticated tenant or permission grant.
 */
export function c0201NombreBrandingSeguro(value: unknown, maxLength = 120): string | null {
  if (typeof value !== 'string') return null;
  const normalized = value.trim();
  return normalized.length > 0 && normalized.length <= maxLength &&
    !/[\u0000-\u001f\u007f<>]/.test(normalized) ? normalized : null;
}

export function c0201LogoBrandingSeguro(value: unknown): string {
  if (typeof value !== 'string' || value.length > 2048) return '';
  const candidate = value.trim();
  if (/^\/(?!\/)[^\s\\<>]*$/.test(candidate)) return candidate;
  try {
    const url = new URL(candidate);
    return url.protocol === 'https:' && url.hostname.length > 0 &&
      !url.username && !url.password ? url.href : '';
  } catch {
    return '';
  }
}

@Injectable({ providedIn: 'root' })
export class EmpresaIdentidadService {
  private readonly _config = signal<EmpresaConfiguracion>(DEFAULT_CONFIG);
  private cargada = false;
  private versionIdentidad = 0;
  // Solo el tenant validado por la API puede alimentar el branding privado.
  private tenantCargado: number | null = null;

  readonly config = this._config.asReadonly();
  readonly nombreSistema = computed(() => c0201NombreBrandingSeguro(this._config().nombreVisibleSistema) ||
    c0201NombreBrandingSeguro(this._config().nombreComercial) || 'SOLQARYN');
  readonly descripcionSistema = computed(() => this.normalizarDescripcion(this._config().encabezadoTexto || this._config().descripcionSistema));
  readonly logoUrl = computed(() => c0201LogoBrandingSeguro(this._config().logoUrl));
  readonly mensajeLogin = computed(() => c0201NombreBrandingSeguro(this._config().mensajeLogin, 180) ||
    `Inicia sesión para administrar ${this.nombreSistema()}`);
  readonly mostrarCopyright = computed(() => this._config().mostrarCopyright);
  readonly copyright = computed(() => {
    const actual = this._config();
    if (!actual.usarAnioAutomaticoCopyright) return actual.copyright;
    return actual.copyright.replace(/\b20\d{2}\b/, String(new Date().getFullYear()));
  });

  constructor(private empresaService: EmpresaConfiguracionService) {}

  cargar(force = false) {
    if (this.cargada && !force && this.tenantCargado === null) return of(this._config());
    const version = ++this.versionIdentidad;
    this.tenantCargado = null;
    this._config.set({ ...DEFAULT_CONFIG });
    return this.empresaService.getPublica().pipe(
      switchMap((res) => {
        const config = { ...DEFAULT_CONFIG, ...res.data };
        if (config.whatsApp?.trim()) return of(config);

        return this.empresaService.getWhatsAppPublico().pipe(
          map(contacto => ({
            ...config,
            whatsApp: contacto.disponible && contacto.numeroTelefonoE164
              ? contacto.numeroTelefonoE164
              : undefined
          })),
          catchError(() => of(config))
        );
      }),
      map((config) => {
        // Una respuesta iniciada en A no puede restaurar nombre/logo o datos
        // empresariales después de limpiar A y seleccionar B.
        if (version === this.versionIdentidad) {
          this._config.set(config);
          this.cargada = true;
        }
        return this._config();
      }),
      catchError(() => {
        if (version === this.versionIdentidad) this.cargada = true;
        return of(this._config());
      })
    );
  }

  /**
   * C14-01.M04.03: branding privado de origen tenant verificado. Esta llamada
   * solo la invoca el shell tras la validacion de membresia por TenantContextService;
   * el backend vuelve a aplicar JWT/RBAC y membresia actual sobre GET /tenant/{id}.
   * Fallos, id ambiguo o respuesta tardia fallan cerrado a la marca neutra.
   */
  cargarTenant(empresaId: number) {
    if (!Number.isSafeInteger(empresaId) || empresaId <= 0) {
      this.usarPlataforma();
      return of(this._config());
    }
    if (this.cargada && this.tenantCargado === empresaId) return of(this._config());

    const version = ++this.versionIdentidad;
    this.tenantCargado = empresaId;
    this.cargada = false;
    // No mostrar la marca anterior mientras el servidor valida el contexto nuevo.
    this._config.set({ ...DEFAULT_CONFIG });
    return this.empresaService.getTenant(empresaId).pipe(
      map(respuesta => {
        if (version !== this.versionIdentidad || this.tenantCargado !== empresaId) {
          return this._config();
        }
        const dto = respuesta?.success === true && respuesta.data?.empresaId === empresaId
          ? respuesta.data : null;
        const nombre = c0201NombreBrandingSeguro(dto?.nombre);
        if (!dto || !nombre) throw new Error('Branding tenant no autorizado o malformado.');
        // No copiar fiscalidad, destinatarios, preferencias ni otros datos privados
        // de ConfigEmpresaTenant al modelo publico compartido.
        this._config.set({
          ...DEFAULT_CONFIG,
          nombreComercial: nombre,
          nombreVisibleSistema: nombre,
          mensajeLogin: `Inicia sesión para administrar ${nombre}`,
          logoUrl: c0201LogoBrandingSeguro(dto.logoUrl)
        });
        this.cargada = true;
        return this._config();
      }),
      catchError(() => {
        if (version === this.versionIdentidad && this.tenantCargado === empresaId) {
          this._config.set({ ...DEFAULT_CONFIG });
          this.cargada = false;
        }
        return of(this._config());
      })
    );
  }

  usarPlataforma(): void {
    this.versionIdentidad++;
    this.tenantCargado = null;
    this._config.set({ ...DEFAULT_CONFIG });
    this.cargada = false;
  }

  refrescarDespuesDeGuardar(config: EmpresaConfiguracion): void {
    this.versionIdentidad++;
    this.tenantCargado = null;
    this._config.set({ ...DEFAULT_CONFIG, ...config });
    this.cargada = true;
  }

  private normalizarDescripcion(value?: string | null): string {
    const texto = c0201NombreBrandingSeguro(value, 200);
    if (!texto || texto === 'Gestión de Inventario' || texto === 'Gestion de Inventario') {
      return 'Administrativo';
    }

    return texto;
  }
}
