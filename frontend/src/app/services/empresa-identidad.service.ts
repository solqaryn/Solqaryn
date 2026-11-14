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

@Injectable({ providedIn: 'root' })
export class EmpresaIdentidadService {
  private readonly _config = signal<EmpresaConfiguracion>(DEFAULT_CONFIG);
  private cargada = false;
  private versionIdentidad = 0;

  readonly config = this._config.asReadonly();
  readonly nombreSistema = computed(() => this._config().nombreVisibleSistema || this._config().nombreComercial || 'SOLQARYN');
  readonly descripcionSistema = computed(() => this.normalizarDescripcion(this._config().encabezadoTexto || this._config().descripcionSistema));
  readonly logoUrl = computed(() => this._config().logoUrl || '');
  readonly mensajeLogin = computed(() => this._config().mensajeLogin || `Inicia sesión para administrar ${this.nombreSistema()}`);
  readonly mostrarCopyright = computed(() => this._config().mostrarCopyright);
  readonly copyright = computed(() => {
    const actual = this._config();
    if (!actual.usarAnioAutomaticoCopyright) return actual.copyright;
    return actual.copyright.replace(/\b20\d{2}\b/, String(new Date().getFullYear()));
  });

  constructor(private empresaService: EmpresaConfiguracionService) {}

  cargar(force = false) {
    if (this.cargada && !force) return of(this._config());
    const version = ++this.versionIdentidad;
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

  usarPlataforma(): void {
    this.versionIdentidad++;
    this._config.set({ ...DEFAULT_CONFIG });
    this.cargada = false;
  }

  refrescarDespuesDeGuardar(config: EmpresaConfiguracion): void {
    this.versionIdentidad++;
    this._config.set({ ...DEFAULT_CONFIG, ...config });
    this.cargada = true;
  }

  private normalizarDescripcion(value?: string | null): string {
    const texto = value?.trim();
    if (!texto || texto === 'Gestión de Inventario' || texto === 'Gestion de Inventario') {
      return 'Administrativo';
    }

    return texto;
  }
}
