import { Injectable } from '@angular/core';
import { TemaVisualService } from './tema-visual.service';
import { CAMPOS_TEMA, TemaVisual } from '../core/models/tema-visual.model';
import { ColorContrastService } from './color-contrast.service';

// El DTO describe colores, no CSS libre: solo se permiten literales hexadecimales.
const COLOR_HEX_SEGURO = /^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i;

@Injectable({ providedIn: 'root' })
export class ThemeApplierService {
  private versionTema = 0;

  constructor(
    private temaVisualService: TemaVisualService,
    private colorContrast: ColorContrastService
  ) {}

  aplicarTemaGuardado(): void {
    // Limpiar de inmediato el branding del tenant anterior mientras llega la
    // configuración del contexto nuevo; una respuesta tardía no debe revertirlo.
    const version = ++this.versionTema;
    this.limpiarTokens();
    this.temaVisualService.get().subscribe({
      next: (res) => {
        if (version === this.versionTema && res?.data) this.aplicar(res.data);
      },
      error: () => {
        // La interfaz conserva los tokens seguros definidos en styles.scss.
      }
    });
  }

  aplicarTemaPlataforma(): void {
    ++this.versionTema;
    this.limpiarTokens();
  }

  aplicar(tema: TemaVisual): void {
    ++this.versionTema;
    this.limpiarTokens();
    if (!tema) return;

    const root = document.documentElement;
    const surface = this.colorSeguro(tema.fondoTarjetas) ?? '#ffffff';
    const page = this.colorSeguro(tema.fondoPrincipal) ?? surface;

    for (const campo of CAMPOS_TEMA) {
      const requested = this.colorSeguro(tema[campo.clave]);
      if (!requested) continue;

      let value = requested;
      if (campo.variableCss === '--color-text' ||
          campo.variableCss === '--color-text-muted' ||
          campo.variableCss === '--color-heading') {
        value = this.colorContrast.ensureReadable(value, [surface, page]);
      }
      root.style.setProperty(campo.variableCss, value);
    }

    const fondos: Array<[string, string | undefined]> = [
      ['--color-on-primary', this.colorSeguro(tema.botonesPrincipales)],
      ['--color-on-sidebar', this.colorSeguro(tema.menuLateral)],
      ['--color-on-danger', this.colorSeguro(tema.colorError)],
      ['--color-on-success', this.colorSeguro(tema.colorExito)],
      ['--color-on-warning', this.colorSeguro(tema.colorAdvertencia)],
      ['--color-on-info', this.colorSeguro(tema.colorInformacion)]
    ];
    for (const [variable, fondo] of fondos) {
      if (fondo) root.style.setProperty(variable, this.colorContrast.foregroundFor(fondo));
    }
  }

  private colorSeguro(valor: unknown): string | undefined {
    if (typeof valor !== 'string') return undefined;
    const color = valor.trim();
    return COLOR_HEX_SEGURO.test(color) ? color : undefined;
  }

  private limpiarTokens(): void {
    const root = document.documentElement;
    for (const campo of CAMPOS_TEMA) root.style.removeProperty(campo.variableCss);
    for (const variable of [
      '--color-on-primary', '--color-on-sidebar', '--color-on-danger',
      '--color-on-success', '--color-on-warning', '--color-on-info'
    ]) root.style.removeProperty(variable);
  }
}
