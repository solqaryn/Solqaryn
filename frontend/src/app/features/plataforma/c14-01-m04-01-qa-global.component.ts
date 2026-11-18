import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C14-01.M04.01 — UI QA privada, nunca fuente de autorizacion ni de PASS. */
@Component({
  selector: 'app-c1401-qa-global',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './c14-01-m04-01-qa-global.component.html',
  styleUrl: './c14-01-m04-01-qa-global.component.scss'
})
export class C1401QaGlobalComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  readonly autorizado = computed(() => {
    const s = this.plataforma.sesion();
    return s?.ambito === 'Plataforma' && s.esAdministrador === true;
  });
  readonly controles = [
    { id: 'scope', titulo: 'Gobierno y procedencia', alcance: 'Scope Lock STRICT y procedencia V5 del SHA exacto.' },
    { id: 'backend', titulo: 'Backend y aislamiento', alcance: '.NET Release, tests y rechazos de tenant A/B, JWT y RBAC cuando correspondan.' },
    { id: 'frontend', titulo: 'Frontend y experiencia', alcance: 'Angular tipado, build y cobertura de rutas con guards.' },
    { id: 'browser', titulo: 'Navegador y accesibilidad', alcance: 'Playwright/Chromium, teclado, estados y responsive cuando aplique.' },
    { id: 'mysql', titulo: 'Persistencia e integración', alcance: 'MySQL 8.4 efímero, migraciones y tests Category=Integration cuando correspondan.' }
  ] as const;
}
