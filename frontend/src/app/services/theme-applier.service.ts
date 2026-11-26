import { Injectable } from '@angular/core';
import { TemaVisualService } from './tema-visual.service';
import { CAMPOS_TEMA, TemaVisual } from '../core/models/tema-visual.model';
import { ColorContrastService } from './color-contrast.service';

@Injectable({ providedIn: 'root' })
export class ThemeApplierService {
  private revision = 0;
  constructor(
    private temaVisualService: TemaVisualService,
    private colorContrast: ColorContrastService
  ) {}

  aplicarTemaGuardado(): void {
    const revision = ++this.revision;
    this.temaVisualService.get().subscribe({
      next: (res) => {
        if (revision === this.revision && res.success && res.data) this.aplicar(res.data);
      },
      error: () => {
        // La interfaz conserva los tokens seguros definidos en styles.scss.
      }
    });
  }

  aplicarTemaPlataforma(): void {
    ++this.revision;
    const root = document.documentElement;
    for (const campo of CAMPOS_TEMA) root.style.removeProperty(campo.variableCss);
    for (const variable of [
      '--color-on-primary', '--color-on-secondary', '--color-on-sidebar', '--color-on-topbar', '--color-on-danger',
      '--color-on-success', '--color-on-warning', '--color-on-info', '--color-control-focus'
    ]) {
      root.style.removeProperty(variable);
    }
  }

  aplicar(tema: TemaVisual): void {
    ++this.revision;
    const root = document.documentElement;
    const surface = tema.fondoTarjetas?.trim() || '#ffffff';
    const page = tema.fondoPrincipal?.trim() || '#f3f7fb';

    for (const campo of CAMPOS_TEMA) {
      const requested = tema[campo.clave];
      if (!requested?.trim()) {
        root.style.removeProperty(campo.variableCss);
        continue;
      }

      let value = requested as string;
      if (campo.variableCss === '--color-text' ||
          campo.variableCss === '--color-text-muted' ||
          campo.variableCss === '--color-heading') {
        value = this.colorContrast.ensureReadable(value, [surface, page]);
      }
      root.style.setProperty(campo.variableCss, value.trim());
    }

    root.style.setProperty('--color-control-focus', this.colorContrast.ensureReadable(tema.colorPrimario || '#0369a1', [surface]));
    root.style.setProperty('--color-on-primary', this.colorContrast.foregroundFor(tema.botonesPrincipales?.trim() || '#0369a1'));
    root.style.setProperty('--color-on-secondary', this.colorContrast.foregroundFor(tema.colorSecundario || '#075985'));
    root.style.setProperty('--color-on-sidebar', this.colorContrast.foregroundFor(tema.menuLateral?.trim() || '#08131f'));
    root.style.setProperty('--color-on-topbar', this.colorContrast.foregroundFor(tema.barraSuperior?.trim() || '#ffffff'));
    root.style.setProperty('--color-on-danger', this.colorContrast.foregroundFor(tema.colorError?.trim() || '#dc2626'));
    root.style.setProperty('--color-on-success', this.colorContrast.foregroundFor(tema.colorExito?.trim() || '#059669'));
    root.style.setProperty('--color-on-warning', this.colorContrast.foregroundFor(tema.colorAdvertencia?.trim() || '#d97706'));
    root.style.setProperty('--color-on-info', this.colorContrast.foregroundFor(tema.colorInformacion?.trim() || '#2563eb'));
  }
}
