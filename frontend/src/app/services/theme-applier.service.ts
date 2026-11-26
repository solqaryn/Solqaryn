import { Injectable } from '@angular/core';
import { TemaVisualService } from './tema-visual.service';
import { CAMPOS_TEMA, TemaVisual } from '../core/models/tema-visual.model';
import { ColorContrastService } from './color-contrast.service';

@Injectable({ providedIn: 'root' })
export class ThemeApplierService {
  constructor(
    private temaVisualService: TemaVisualService,
    private colorContrast: ColorContrastService
  ) {}

  aplicarTemaGuardado(): void {
    this.temaVisualService.get().subscribe({
      next: (res) => this.aplicar(res.data),
      error: () => {
        // La interfaz conserva los tokens seguros definidos en styles.scss.
      }
    });
  }

  aplicarTemaPlataforma(): void {
    const root = document.documentElement;
    for (const campo of CAMPOS_TEMA) root.style.removeProperty(campo.variableCss);
    for (const variable of [
      '--color-on-primary', '--color-on-secondary', '--color-on-sidebar', '--color-on-danger',
      '--color-on-success', '--color-on-warning', '--color-on-info'
    ]) {
      root.style.removeProperty(variable);
    }
  }

  aplicar(tema: TemaVisual): void {
    const root = document.documentElement;
    const surface = tema.fondoTarjetas || '#ffffff';
    const page = tema.fondoPrincipal || surface;

    for (const campo of CAMPOS_TEMA) {
      const requested = tema[campo.clave];
      if (!requested) continue;

      let value = requested as string;
      if (campo.variableCss === '--color-text' ||
          campo.variableCss === '--color-text-muted' ||
          campo.variableCss === '--color-heading') {
        value = this.colorContrast.ensureReadable(value, [surface, page]);
      }
      root.style.setProperty(campo.variableCss, value.trim());
    }

    root.style.setProperty('--color-on-primary', this.colorContrast.foregroundFor(tema.botonesPrincipales));
    root.style.setProperty('--color-on-secondary', this.colorContrast.foregroundFor(tema.colorSecundario || '#075985'));
    root.style.setProperty('--color-on-sidebar', this.colorContrast.foregroundFor(tema.menuLateral));
    root.style.setProperty('--color-on-danger', this.colorContrast.foregroundFor(tema.colorError));
    root.style.setProperty('--color-on-success', this.colorContrast.foregroundFor(tema.colorExito));
    root.style.setProperty('--color-on-warning', this.colorContrast.foregroundFor(tema.colorAdvertencia));
    root.style.setProperty('--color-on-info', this.colorContrast.foregroundFor(tema.colorInformacion));
  }
}
