import { Injectable, computed, signal } from '@angular/core';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { EmpresaConfiguracion } from '../../core/models/empresa-configuracion.model';
import { EmpresaConfiguracionService } from '../../services/empresa-configuracion.service';
import { TiendaBootstrapPublico } from './varistorehn.models';
import { VaristorehnService } from './varistorehn.service';

const STORE_DEFAULT_CONFIG: EmpresaConfiguracion = {
  id: 0,
  nombreComercial: 'Tienda',
  eslogan: '',
  nombreVisibleSistema: 'Tienda',
  descripcionSistema: '',
  mensajeLogin: '',
  copyright: '',
  mostrarCopyright: false,
  usarAnioAutomaticoCopyright: true,
  encabezadoActivo: false,
  piePaginaActivo: false,
  moneda: 'HNL',
  zonaHoraria: 'America/Tegucigalpa',
  formatoFecha: 'dd/MM/yyyy'
};

/**
 * Identidad comercial exclusiva del storefront.
 *
 * La ruta pública nunca hereda el fallback SOLQARYN del shell de plataforma.
 * La autoridad de nombre, logo, eslogan, contacto y moneda sigue siendo la
 * configuración pública persistida; si el backend no está disponible, la UI
 * degrada a una identidad neutra en lugar de presentar otra empresa.
 */
@Injectable({ providedIn: 'root' })
export class VaristorehnIdentidadService {
  private readonly _config = signal<EmpresaConfiguracion>(STORE_DEFAULT_CONFIG);
  private readonly _bootstrap = signal<TiendaBootstrapPublico | null>(null);
  private cargada = false;

  readonly config = this._config.asReadonly();
  readonly bootstrap = this._bootstrap.asReadonly();
  readonly nombreSistema = computed(() => this._config().nombreComercial || 'Tienda');
  readonly descripcionSistema = computed(() =>
    (this._config().encabezadoTexto || this._config().descripcionSistema || '').trim()
  );
  readonly logoUrl = computed(() => this._config().logoUrl || '');
  readonly mensajeLogin = computed(() => this._config().mensajeLogin || '');
  readonly mostrarCopyright = computed(() => this._config().mostrarCopyright);
  readonly copyright = computed(() => {
    const actual = this._config();
    if (!actual.usarAnioAutomaticoCopyright) return actual.copyright;
    return actual.copyright.replace(/\b20\d{2}\b/, String(new Date().getFullYear()));
  });

  constructor(
    private tiendaService: VaristorehnService,
    private empresaService: EmpresaConfiguracionService
  ) {}

  cargar(force = false) {
    if (this.cargada && !force) return of(this._bootstrap());

    return this.tiendaService.obtenerBootstrap(force).pipe(
      tap((bootstrap) => this.aplicarBootstrap(bootstrap)),
      catchError(() => this.cargarLegacy())
    );
  }

  private aplicarBootstrap(bootstrap: TiendaBootstrapPublico): void {
    this._bootstrap.set(bootstrap);
    this._config.set({
      ...STORE_DEFAULT_CONFIG,
      ...bootstrap.identidad,
      whatsApp: bootstrap.identidad.whatsApp || undefined,
      logoUrl: bootstrap.identidad.logoUrl || undefined,
      telefono: bootstrap.identidad.telefono || undefined,
      correo: bootstrap.identidad.correo || undefined,
      encabezadoTexto: bootstrap.identidad.encabezadoTexto || undefined,
      piePaginaTexto: bootstrap.identidad.piePaginaTexto || undefined
    });
    this.cargada = true;
  }

  private cargarLegacy() {
    return this.empresaService.getPublica().pipe(
      switchMap((res) => {
        const config = { ...STORE_DEFAULT_CONFIG, ...res.data };
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
      tap((config) => {
        this._bootstrap.set(null);
        this._config.set(config);
        this.cargada = true;
      }),
      map(() => null),
      catchError(() => {
        this._bootstrap.set(null);
        this._config.set(STORE_DEFAULT_CONFIG);
        this.cargada = true;
        return of(null);
      })
    );
  }
}
