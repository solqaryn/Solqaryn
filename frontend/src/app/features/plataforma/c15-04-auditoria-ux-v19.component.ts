import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * C15-04.M04.01 — contrato visual privado de auditoria UX.
 * No es telemetria en vivo ni una certificacion WCAG o de rendimiento.
 * El acceso lo controla plataformaGuard del router.
 */
@Component({
  selector: 'app-c1504-auditoria-ux-v19',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './c15-04-auditoria-ux-v19.component.html',
  styleUrl: './c15-04-auditoria-ux-v19.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class C1504AuditoriaUxV19Component {
  readonly superficies = [
    { id: 'landing', titulo: 'Landing', objetivo: 'Lectura inicial, accesibilidad, formularios y carga visual.' },
    { id: 'portal', titulo: 'Portal', objetivo: 'Estados, permisos, teclado y navegación autenticada.' },
    { id: 'erp', titulo: 'ERP', objetivo: 'Tablas, densidad, navegación y respuesta en pantallas pequeñas.' },
    { id: 'builder', titulo: 'Builder', objetivo: 'Controles, previsualización, foco y configuración segura.' },
    { id: 'global', titulo: 'Consola global', objetivo: 'Superficies privadas, contraste, errores y carga progresiva.' }
  ] as const;
  readonly anchosObjetivo = [320, 375, 768, 1280] as const;
}
